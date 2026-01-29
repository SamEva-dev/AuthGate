using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.AcceptInvitation;
using AuthGate.Auth.Application.Features.Auth.Commands.InviteCollaborator;
using AuthGate.Auth.Application.Features.Auth.Commands.Login;
using AuthGate.Auth.Application.Features.Auth.Commands.AcceptLocaGuestInvitation;
using AuthGate.Auth.Application.Features.Auth.Commands.RefreshToken;
using AuthGate.Auth.Application.Features.Auth.Commands.Register;
using AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;
using AuthGate.Auth.Application.Features.Auth.Commands.ValidateEmail;
using AuthGate.Auth.Application.Features.Auth.Commands.Verify2FA;
using AuthGate.Auth.Application.Features.Auth.Commands.VerifyRecoveryCode;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using AuthGate.Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using LocaGuest.Emailing.Abstractions;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly ILocaGuestProvisioningClient _locaGuest;

    public AuthController(
        IMediator mediator, 
        ILogger<AuthController> logger,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        IUnitOfWork unitOfWork,
        AuthDbContext db,
        IConfiguration configuration,
        IAuditService auditService,
        ILocaGuestProvisioningClient locaGuest)
    {
        _mediator = mediator;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _userRoleService = userRoleService;
        _unitOfWork = unitOfWork;
        _db = db;
        _configuration = configuration;
        _auditService = auditService;
        _locaGuest = locaGuest;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("invitations/accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AcceptLocaGuestInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptLocaGuestInvitation([FromBody] AcceptLocaGuestInvitationCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("prelogin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PreLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreLogin([FromBody] PreLoginRequestDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { error = "Email is required." });

        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            return Ok(new PreLoginResponseDto
            {
                NextStep = "Register"
            });
        }

        if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isSuperAdmin = roles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));

            if (!isSuperAdmin)
            {
                return Ok(new PreLoginResponseDto
                {
                    NextStep = "Error",
                    Error = "Account has no organization assigned. Please contact support or use an invitation link."
                });
            }
        }

        return Ok(new PreLoginResponseDto
        {
            NextStep = "Password"
        });
    }

    /// <summary>
    /// Register a new organization owner (TenantOwner) with automatic organization creation
    /// This creates both an organization in LocaGuest and a user in AuthGate
    /// </summary>
    [HttpPost("register-with-tenant")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterWithTenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterWithTenant([FromBody] RegisterWithTenantCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("validate-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidateEmail([FromBody] ValidateEmailCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    public record ResendConfirmEmailRequest(string Email);

    [HttpPost("resend-confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmEmail([FromBody] ResendConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "Email is required." });

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Avoid user enumeration
            return Ok(new { success = true });
        }

        if (user.EmailConfirmed)
        {
            return Ok(new { success = true });
        }

        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var frontendUrl = _configuration["Frontend:ConfirmEmailUrl"] ?? "http://localhost:4200/confirm-email";
        var verifyUrl = $"{frontendUrl}?token={Uri.EscapeDataString(confirmToken)}&email={Uri.EscapeDataString(user.Email!)}";

        var firstName = user.FirstName ?? string.Empty;
        var subject = "Vérifiez votre adresse email";
        var htmlBody = $$"""
<h2>✉️ Vérification d'email</h2>
<p>Bonjour {{firstName}},</p>
<p>Pour finaliser votre inscription, veuillez vérifier votre adresse email en cliquant sur le bouton ci-dessous :</p>
<p><a href="{{verifyUrl}}">Vérifier mon email</a></p>
<p>Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>
""";

        await _emailing.QueueHtmlAsync(
            toEmail: user.Email!,
            subject: subject,
            htmlContent: htmlBody,
            textContent: null,
            attachments: null,
            tags: EmailUseCaseTags.AuthConfirmEmail,
            cancellationToken: cancellationToken);

        return Ok(new { success = true });
    }

    [HttpGet("provisioning-status")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvisioningStatus([FromQuery] string email)
    {
        var normalized = (email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return BadRequest(new { error = "Email is required." });

        var user = await _userManager.FindByEmailAsync(normalized);
        if (user == null)
            return NotFound(new { error = "User not found" });

        return Ok(new
        {
            status = user.Status.ToString(),
            organizationId = user.OrganizationId
        });
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Verify 2FA/TOTP code and complete login
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FACommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Verify 2FA recovery code and complete login
    /// </summary>
    [HttpPost("verify-recovery-code")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyRecoveryCode([FromBody] VerifyRecoveryCodeCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = dto.RefreshToken,
            AccessToken = dto.AccessToken
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        // TODO: Implement logout command to revoke refresh token
        return NoContent();
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Essayer d'abord avec 'sub' (standard JWT), puis avec ClaimTypes.NameIdentifier
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No user ID found in claims. Available claims: {Claims}", 
                string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Get user permissions from roles
        var permissions = new List<string>();
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null && role.RolePermissions != null)
            {
                permissions.AddRange(role.RolePermissions.Select(rp => rp.Permission.Code));
            }
        }

        var currentUserDto = new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            MfaEnabled = user.MfaEnabled,
            Roles = roles.ToList(),
            Permissions = permissions.Distinct().ToList(),
            CreatedAt = user.CreatedAtUtc,
            LastLoginAt = user.LastLoginAtUtc
        };

        return Ok(currentUserDto);
    }

    [HttpGet("me/organizations")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [ProducesResponseType(typeof(IEnumerable<UserOrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrganizations(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var roles = await _userRoleService.GetUserRolesAsync(user);
        var isPlatformAdmin = roles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        var isTenantOwnerOrAdmin = roles.Any(r =>
            string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.TenantOwner, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.TenantAdmin, StringComparison.OrdinalIgnoreCase));

        if (isPlatformAdmin)
        {
            var all = await _locaGuest.GetOrganizationsAsync(cancellationToken);
            var dtosAll = all
                .Select(o => new UserOrganizationDto
                {
                    OrganizationId = o.Id,
                    Name = o.Name,
                    Role = AuthGate.Auth.Domain.Constants.Roles.SuperAdmin,
                    IsDefault = user.OrganizationId.HasValue && user.OrganizationId.Value == o.Id
                })
                .OrderBy(x => x.Name)
                .ToList();

            return Ok(dtosAll);
        }

        if (isTenantOwnerOrAdmin)
        {
            if (!user.OrganizationId.HasValue)
                return Ok(Array.Empty<UserOrganizationDto>());

            var orgId = user.OrganizationId.Value;
            string? name = null;

            // Prefer cached display name if present
            var cached = await _db.UserOrganizations
                .AsNoTracking()
                .Where(x => x.UserId == userGuid && x.OrganizationId == orgId)
                .Select(x => x.OrganizationDisplayName)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(cached))
            {
                name = cached;
            }
            else
            {
                try
                {
                    var org = await _locaGuest.GetOrganizationByIdAsync(orgId, cancellationToken);
                    name = org?.Name;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to resolve organization name for {OrganizationId}", orgId);
                }
            }

            return Ok(new List<UserOrganizationDto>
            {
                new UserOrganizationDto
                {
                    OrganizationId = orgId,
                    Name = name,
                    Role = roles.FirstOrDefault(),
                    IsDefault = true
                }
            });
        }

        var links = await _unitOfWork.UserOrganizations.GetByUserIdAsync(userGuid, cancellationToken);
        var linkList = links.ToList();

        var orgIds = linkList.Select(x => x.OrganizationId).Distinct().ToList();

        var missingNameOrgIds = linkList
            .Where(x => string.IsNullOrWhiteSpace(x.OrganizationDisplayName))
            .Select(x => x.OrganizationId)
            .Distinct()
            .ToList();

        var lookups = await Task.WhenAll(missingNameOrgIds.Select(async id =>
        {
            try
            {
                var org = await _locaGuest.GetOrganizationByIdAsync(id, cancellationToken);
                return (id, name: org?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to resolve organization name for {OrganizationId}", id);
                return (id, name: (string?)null);
            }
        }));

        var nameById = lookups
            .Where(x => !string.IsNullOrWhiteSpace(x.name))
            .ToDictionary(x => x.id, x => x.name!, EqualityComparer<Guid>.Default);

        if (nameById.Count > 0)
        {
            var toUpdate = await _db.UserOrganizations
                .Where(x => x.UserId == userGuid && nameById.Keys.Contains(x.OrganizationId) && x.OrganizationDisplayName == null)
                .ToListAsync(cancellationToken);

            foreach (var link in toUpdate)
            {
                if (nameById.TryGetValue(link.OrganizationId, out var n))
                    link.OrganizationDisplayName = n;
            }

            if (toUpdate.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);
        }

        var dtos = linkList
            .Select(x => new UserOrganizationDto
            {
                OrganizationId = x.OrganizationId,
                Name = !string.IsNullOrWhiteSpace(x.OrganizationDisplayName)
                    ? x.OrganizationDisplayName
                    : (nameById.TryGetValue(x.OrganizationId, out var n) ? n : null),
                Role = x.RoleInOrg,
                IsDefault = user.OrganizationId.HasValue && user.OrganizationId.Value == x.OrganizationId
            })
            .ToList();

        return Ok(dtos);
    }

    [HttpPost("switch-organization")]
    [Authorize]
    [ProducesResponseType(typeof(SwitchOrganizationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SwitchOrganization([FromBody] SwitchOrganizationRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto == null || dto.OrganizationId == Guid.Empty)
        {
            return BadRequest(new { error = "organizationId is required" });
        }

        var pwdClaim = User.FindFirstValue("pwd_change_required");
        if (!string.IsNullOrWhiteSpace(pwdClaim) && string.Equals(pwdClaim, "true", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var passwordChangeExpired = user.MustChangePasswordBeforeUtc != null && DateTime.UtcNow > user.MustChangePasswordBeforeUtc.Value;
        if (user.MustChangePassword || passwordChangeExpired)
        {
            return Forbid();
        }

        var roles = await _userRoleService.GetUserRolesAsync(user);
        var permissions = await _userRoleService.GetUserPermissionsAsync(user);
        var isPlatformAdmin = roles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));

        var fromOrgId = User.FindFirstValue("org_id") ?? User.FindFirstValue("organization_id");

        if (!isPlatformAdmin)
        {
            var hasLink = await _unitOfWork.UserOrganizations.ExistsAsync(userGuid, dto.OrganizationId, cancellationToken);
            if (!hasLink)
            {
                return Forbid();
            }
        }

        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userGuid, "Organization context switched", cancellationToken);

        var app = User.FindFirstValue("app") ?? "locaguest";

        string accessToken;
        if (isPlatformAdmin)
        {
            accessToken = _jwtService.GeneratePlatformAccessToken(user.Id, user.Email!, roles, permissions, user.MfaEnabled, pwdChangeRequired: false, organizationId: dto.OrganizationId, app: app);
        }
        else
        {
            accessToken = _jwtService.GenerateTenantAccessToken(user.Id, user.Email!, roles, permissions, user.MfaEnabled, dto.OrganizationId, pwdChangeRequired: false, app: app);
        }

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();

        await _unitOfWork.RefreshTokens.AddAsync(new AuthGate.Auth.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenHash,
            JwtId = jwtId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var auditMetadata = JsonSerializer.Serialize(new
        {
            fromOrgId,
            toOrgId = dto.OrganizationId,
            platform_admin = isPlatformAdmin
        });

        await _auditService.LogAsync(
            user.Id,
            AuthGate.Auth.Domain.Enums.AuditAction.OrganizationContextSwitched,
            description: "Organization context switched",
            isSuccess: true,
            metadata: auditMetadata,
            cancellationToken: cancellationToken);

        return Ok(new SwitchOrganizationResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            OrganizationId = dto.OrganizationId
        });
    }

    [HttpPost("clear-organization-context")]
    [Authorize]
    [ProducesResponseType(typeof(SwitchOrganizationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ClearOrganizationContext(CancellationToken cancellationToken)
    {
        var pwdClaim = User.FindFirstValue("pwd_change_required");
        if (!string.IsNullOrWhiteSpace(pwdClaim) && string.Equals(pwdClaim, "true", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var passwordChangeExpired = user.MustChangePasswordBeforeUtc != null && DateTime.UtcNow > user.MustChangePasswordBeforeUtc.Value;
        if (user.MustChangePassword || passwordChangeExpired)
        {
            return Forbid();
        }

        var roles = await _userRoleService.GetUserRolesAsync(user);
        var permissions = await _userRoleService.GetUserPermissionsAsync(user);
        var isPlatformAdmin = roles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        if (!isPlatformAdmin)
        {
            return Forbid();
        }

        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(userGuid, "Organization context cleared", cancellationToken);

        var app = User.FindFirstValue("app") ?? "locaguest";

        var accessToken = _jwtService.GeneratePlatformAccessToken(user.Id, user.Email!, roles, permissions, user.MfaEnabled, pwdChangeRequired: false, app: app);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();

        await _unitOfWork.RefreshTokens.AddAsync(new AuthGate.Auth.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenHash,
            JwtId = jwtId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var fromOrgId = User.FindFirstValue("org_id") ?? User.FindFirstValue("organization_id");
        var auditMetadata = JsonSerializer.Serialize(new
        {
            fromOrgId,
            toOrgId = (Guid?)null,
            platform_admin = true
        });

        await _auditService.LogAsync(
            user.Id,
            AuthGate.Auth.Domain.Enums.AuditAction.OrganizationContextSwitched,
            description: "Organization context cleared",
            isSuccess: true,
            metadata: auditMetadata,
            cancellationToken: cancellationToken);

        return Ok(new SwitchOrganizationResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            OrganizationId = null
        });
    }

    /// <summary>
    /// Invite a collaborator to join the organization
    /// Requires TenantOwner or TenantAdmin role
    /// </summary>
    [HttpPost("invite")]
    [Authorize]
    [ProducesResponseType(typeof(InviteCollaboratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteCollaborator([FromBody] InviteCollaboratorCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Invitation sent to {Email}", command.Email);
        return Ok(result.Value);
    }

    /// <summary>
    /// Accept an invitation and create a user account
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpPost("accept-invitation")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AcceptInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Invitation accepted by {Email}", result.Value.Email);
        return Ok(result.Value);
    }

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        user.MustChangePassword = false;
        user.MustChangePasswordBeforeUtc = null;
        user.PasswordLastChangedAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(user.Id, "Password changed", HttpContext.RequestAborted);

        var roles = await _userRoleService.GetUserRolesAsync(user);
        var permissions = await _userRoleService.GetUserPermissionsAsync(user);

        var isSuperAdminRole = roles.Any(r => string.Equals(r, AuthGate.Auth.Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));
        string accessToken;
        if (isSuperAdminRole)
        {
            accessToken = _jwtService.GeneratePlatformAccessToken(user.Id, user.Email!, roles, permissions, user.MfaEnabled, pwdChangeRequired: false);
        }
        else
        {
            if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
            {
                return BadRequest(new { error = "Cannot issue access token without OrganizationId." });
            }

            accessToken = _jwtService.GenerateTenantAccessToken(user.Id, user.Email!, roles, permissions, user.MfaEnabled, user.OrganizationId.Value, pwdChangeRequired: false);
        }

        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();

        await _unitOfWork.RefreshTokens.AddAsync(new AuthGate.Auth.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenHash,
            JwtId = jwtId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        }, HttpContext.RequestAborted);

        await _unitOfWork.SaveChangesAsync(HttpContext.RequestAborted);

        _logger.LogInformation("Password changed for user {UserId}", userId);
        return Ok(new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            PasswordChangeRequired = false,
            PasswordChangeBeforeUtc = null
        });
    }

    private string HashRefreshToken(string refreshToken)
    {
        var pepper = _configuration["Jwt:RefreshTokenPepper"];
        if (string.IsNullOrWhiteSpace(pepper))
        {
            pepper = _configuration["Security:RefreshTokenPepper"];
        }

        if (string.IsNullOrWhiteSpace(pepper))
        {
            return refreshToken;
        }

        var input = Encoding.UTF8.GetBytes(refreshToken + pepper);
        var hash = SHA256.HashData(input);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Deactivate (soft delete) the current user account
    /// </summary>
    [HttpPost("deactivate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateAccount()
    {
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        user.IsActive = false;
        user.DeactivatedAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Account deactivated for user {UserId}", userId);
        return Ok(new { message = "Account deactivated successfully" });
    }
}
