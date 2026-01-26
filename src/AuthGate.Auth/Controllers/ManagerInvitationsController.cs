using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using AuthGate.Auth.Infrastructure.Persistence;
using LocaGuest.Emailing.Abstractions;
using LocaGuest.Emailing.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AuthGate.Auth.Application.Services;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/manager/invitations")]
[Authorize(Policy = "ManagerAppRequired")]
public class ManagerInvitationsController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailingService _emailing;
    private readonly IConfiguration _configuration;
    private readonly IOrganizationContext _org;

    public ManagerInvitationsController(
        AuthDbContext db,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IUnitOfWork unitOfWork,
        IEmailingService emailing,
        IConfiguration configuration,
        IOrganizationContext org)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _emailing = emailing;
        _configuration = configuration;
        _org = org;
    }

    [HttpPost]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [ProducesResponseType(typeof(CreateManagerInvitationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateManagerInvitationRequestDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || dto.OrganizationId == Guid.Empty)
            return BadRequest(new { error = "email and organizationId are required" });

        var userIdStr = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var inviterId))
            return Unauthorized();

        var callerRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isPlatformAdmin = callerRoles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        var isOrgAdmin = callerRoles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.TenantOwner, StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.TenantAdmin, StringComparison.OrdinalIgnoreCase));

        if (!isPlatformAdmin && !isOrgAdmin)
            return Forbid();

        if (!isPlatformAdmin)
        {
            var ctxOrg = User.FindFirstValue("org_id") ?? User.FindFirstValue("organization_id");
            if (string.IsNullOrWhiteSpace(ctxOrg) || !Guid.TryParse(ctxOrg, out var ctxOrgId) || ctxOrgId != dto.OrganizationId)
                return Forbid();
        }

        var maxExpiresInHours = 168;
        if (int.TryParse(_configuration["ManagerInvitations:MaxExpiresInHours"], out var configuredMax) && configuredMax > 0)
        {
            maxExpiresInHours = configuredMax;
        }

        var expiresInHours = dto.ExpiresInHours <= 0 ? 48 : dto.ExpiresInHours;
        if (expiresInHours > maxExpiresInHours)
        {
            expiresInHours = maxExpiresInHours;
        }

        var expiresIn = TimeSpan.FromHours(expiresInHours);

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        var existingPending = await _db.ManagerInvitations
            .AsNoTracking()
            .Where(x => x.OrganizationId == dto.OrganizationId
                        && x.Email == normalizedEmail
                        && x.Status == ManagerInvitationStatus.Pending
                        && x.ExpiresAtUtc > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (existingPending != null)
        {
            return BadRequest(new { error = "An active invitation already exists for this email" });
        }

        var existingUser = await _userManager.FindByEmailAsync(dto.Email.Trim());
        var invitationType = existingUser == null ? ManagerInvitationType.Activate : ManagerInvitationType.Grant;

        var roleIds = dto.RoleIds ?? new List<Guid>();
        if (roleIds.Count > 0)
        {
            foreach (var roleId in roleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role == null)
                {
                    return BadRequest(new { error = $"Role not found: {roleId}" });
                }
            }
        }

        var (inv, rawToken) = ManagerInvitation.Create(
            organizationId: dto.OrganizationId,
            email: normalizedEmail,
            type: invitationType,
            roleIds: roleIds,
            invitedByUserId: inviterId,
            expiresIn: expiresIn);

        if (existingUser != null)
        {
            inv.ExistingUserId = existingUser.Id;

            await EnsureManagerAccessGrantedAsync(existingUser, dto.OrganizationId, roleIds, inviterId, ct);

            inv.MarkUsed(inviterId);
        }
        else
        {
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                UserName = dto.Email.Trim(),
                Email = dto.Email.Trim(),
                EmailConfirmed = false,
                IsActive = true,
                Status = UserStatus.Active,
                OrganizationId = null,
                MustChangePassword = false
            };

            var createRes = await _userManager.CreateAsync(newUser);
            if (!createRes.Succeeded)
            {
                var errors = string.Join(", ", createRes.Errors.Select(e => e.Description));
                return BadRequest(new { error = errors });
            }

            inv.ExistingUserId = newUser.Id;

            await EnsureManagerAccessGrantedAsync(newUser, dto.OrganizationId, roleIds, inviterId, ct);
        }

        _db.ManagerInvitations.Add(inv);
        await _db.SaveChangesAsync(ct);

        if (dto.SendEmail)
        {
            var frontendUrl = _configuration["ManagerFrontend:BaseUrl"]
                              ?? _configuration["Frontend:BaseUrl"]
                              ?? string.Empty;

            var link = string.IsNullOrWhiteSpace(frontendUrl)
                ? rawToken
                : $"{frontendUrl.TrimEnd('/')}/manager-invitation/{rawToken}";

            var subject = invitationType == ManagerInvitationType.Grant
                ? "Accès au portail Manager"
                : "Activation de votre accès au portail Manager";

            var html = $$"""
<h2>{{subject}}</h2>
<p>Bonjour,</p>
<p>Vous avez reçu un accès au portail <strong>LocaGuest Manager</strong>.</p>
<p><a href=\"{{link}}\">Ouvrir</a></p>
<p style=\"font-size: 14px; color: #6b7280;\">Cette invitation expire le <strong>{{inv.ExpiresAtUtc:dd/MM/yyyy à HH:mm}} UTC</strong></p>
""";

            await _emailing.QueueHtmlAsync(
                toEmail: dto.Email.Trim(),
                subject: subject,
                htmlContent: html,
                textContent: null,
                attachments: null,
                tags: EmailUseCaseTags.AccessInviteUser,
                cancellationToken: ct);
        }

        return Ok(new CreateManagerInvitationResponseDto
        {
            InvitationId = inv.Id,
            Email = inv.Email,
            OrganizationId = inv.OrganizationId,
            RoleIds = roleIds,
            Type = inv.Type.ToString(),
            ExpiresAtUtc = inv.ExpiresAtUtc
        });
    }

    [HttpGet]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(IEnumerable<ManagerInvitationListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var effectiveOrgId = _org.OrganizationId.Value;

        var query = _db.ManagerInvitations.AsNoTracking().AsQueryable();
        query = query.Where(x => x.OrganizationId == effectiveOrgId);

        var rows = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.Email,
                x.OrganizationId,
                x.RoleIdsCsv,
                Type = x.Type.ToString(),
                Status = x.Status.ToString(),
                x.ExpiresAtUtc,
                x.CreatedAtUtc
            })
            .ToListAsync(ct);

        var nowUtc = DateTime.UtcNow;

        var items = rows
            .Select(x => new ManagerInvitationListItemDto
            {
                Id = x.Id,
                Email = x.Email,
                OrganizationId = x.OrganizationId,
                RoleIds = x.RoleIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => Guid.TryParse(v, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty)
                    .ToList(),
                Type = x.Type,
                Status = string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase) && x.ExpiresAtUtc <= nowUtc
                    ? "Expired"
                    : x.Status,
                ExpiresAtUtc = x.ExpiresAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToList();

        return Ok(items);
    }

    [HttpPost("{id:guid}/revoke")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [ProducesResponseType(typeof(RevokeManagerInvitationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Revoke([FromRoute] Guid id, CancellationToken ct)
    {
        var inv = await _db.ManagerInvitations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (inv == null)
            return NotFound();

        var callerRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var isPlatformAdmin = callerRoles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        if (!isPlatformAdmin)
        {
            var ctxOrg = User.FindFirstValue("org_id") ?? User.FindFirstValue("organization_id");
            if (string.IsNullOrWhiteSpace(ctxOrg) || !Guid.TryParse(ctxOrg, out var ctxOrgId) || ctxOrgId != inv.OrganizationId)
                return Forbid();
        }

        var userIdStr = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        Guid? by = Guid.TryParse(userIdStr, out var g) ? g : null;

        inv.Revoke(by);
        await _db.SaveChangesAsync(ct);

        return Ok(new RevokeManagerInvitationResponseDto
        {
            Id = inv.Id,
            Status = inv.Status.ToString()
        });
    }

    [HttpGet("validate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ValidateManagerInvitationResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = "token is required" });

        if (!TryParseToken(token, out var id, out var secret))
            return BadRequest(new { error = "invalid token" });

        var inv = await _db.ManagerInvitations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (inv == null)
            return NotFound();

        if (!inv.IsValid() || !inv.VerifyToken(secret))
            return Forbid();

        return Ok(new ValidateManagerInvitationResponseDto
        {
            Email = inv.Email,
            OrganizationId = inv.OrganizationId,
            RoleIds = inv.RoleIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => Guid.TryParse(v, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList(),
            ExpiresAtUtc = inv.ExpiresAtUtc,
            Type = inv.Type.ToString()
        });
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Accept([FromBody] AcceptManagerInvitationRequestDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { error = "token and newPassword are required" });

        if (!TryParseToken(dto.Token, out var id, out var secret))
            return BadRequest(new { error = "invalid token" });

        var inv = await _db.ManagerInvitations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (inv == null)
            return NotFound();

        if (!inv.IsValid() || !inv.VerifyToken(secret))
            return Forbid();

        if (inv.Type != ManagerInvitationType.Activate)
            return BadRequest(new { error = "Invitation does not require activation" });

        if (!inv.ExistingUserId.HasValue)
            return BadRequest(new { error = "Invitation has no user" });

        var user = await _userManager.FindByIdAsync(inv.ExistingUserId.Value.ToString());
        if (user == null)
            return BadRequest(new { error = "User not found" });

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetRes = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
        if (!resetRes.Succeeded)
        {
            var errors = string.Join(", ", resetRes.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        user.EmailConfirmed = true;
        await _userManager.UpdateAsync(user);

        inv.MarkUsed(user.Id);
        await _db.SaveChangesAsync(ct);

        return Ok(new { message = "Activation completed" });
    }

    private async Task EnsureManagerAccessGrantedAsync(User user, Guid organizationId, IEnumerable<Guid> roleIds, Guid grantedByUserId, CancellationToken ct)
    {
        var already = await _unitOfWork.UserAppAccess.HasAccessAsync(user.Id, "manager", ct);
        if (!already)
        {
            await _unitOfWork.UserAppAccess.AddAsync(new UserAppAccess
            {
                UserId = user.Id,
                AppId = "manager",
                GrantedAtUtc = DateTime.UtcNow,
                GrantedByUserId = grantedByUserId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = grantedByUserId
            }, ct);
        }

        var hasLink = await _unitOfWork.UserOrganizations.ExistsAsync(user.Id, organizationId, ct);
        if (!hasLink)
        {
            await _unitOfWork.UserOrganizations.AddAsync(new UserOrganization
            {
                UserId = user.Id,
                OrganizationId = organizationId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = grantedByUserId
            }, ct);
        }

        foreach (var roleId in roleIds)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
                continue;

            var isInRole = await _userManager.IsInRoleAsync(user, role.Name!);
            if (!isInRole)
            {
                await _userManager.AddToRoleAsync(user, role.Name!);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static bool TryParseToken(string token, out Guid id, out string secret)
    {
        id = Guid.Empty;
        secret = string.Empty;

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
            return false;

        if (!Guid.TryParse(parts[0], out id))
            return false;

        secret = parts[1];
        return !string.IsNullOrWhiteSpace(secret);
    }
}
