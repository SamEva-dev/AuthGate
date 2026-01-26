using AuthGate.Auth.Application.DTOs.Manager;
using AuthGate.Auth.Application.DTOs.Permissions;
using AuthGate.Auth.Application.DTOs.Roles;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Infrastructure.Persistence;
using AuthGate.Auth.Authorization;
using LocaGuest.Emailing.Abstractions;
using LocaGuest.Emailing.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/manager")]
[Authorize(Policy = "ManagerAppRequired")]
public class ManagerController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly IAuditDbContext _audit;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IUserRoleService _userRoleService;
    private readonly IOrganizationContext _org;
    private readonly IConfiguration _configuration;
    private readonly IEmailingService _emailing;

    public ManagerController(
        AuthDbContext db,
        IAuditDbContext audit,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IUserRoleService userRoleService,
        IOrganizationContext org,
        IConfiguration configuration,
        IEmailingService emailing)
    {
        _db = db;
        _audit = audit;
        _userManager = userManager;
        _roleManager = roleManager;
        _userRoleService = userRoleService;
        _org = org;
        _configuration = configuration;
        _emailing = emailing;
    }

    [HttpGet("bootstrap")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(ManagerBootstrapDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBootstrap(CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var userIdStr = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return Unauthorized();

        var myRoles = await _userRoleService.GetUserRolesAsync(user);
        var myPerms = await _userRoleService.GetUserPermissionsAsync(user);

        var permissionsCatalogVersion = 1;
        var permissionsCatalog = await _db.Permissions.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Code)
            .Select(p => new BootstrapPermissionDto
            {
                Key = p.Code,
                Label = p.DisplayName,
                Module = p.Category ?? string.Empty
            })
            .ToListAsync(ct);

        var usersTotal = await _userManager.Users.AsNoTracking().CountAsync(u => u.OrganizationId == orgId, ct);
        var usersActive = await _userManager.Users.AsNoTracking().CountAsync(u => u.OrganizationId == orgId && u.Status == UserStatus.Active, ct);

        var nowUtc = DateTime.UtcNow;
        var invitationsPending = await _db.ManagerInvitations.AsNoTracking()
            .CountAsync(x => x.OrganizationId == orgId && x.Status == ManagerInvitationStatus.Pending && x.ExpiresAtUtc > nowUtc, ct);

        var adminRoleNames = new[] { AuthGate.Auth.Domain.Constants.Roles.TenantOwner, AuthGate.Auth.Domain.Constants.Roles.TenantAdmin };
        var adminRoleIds = await _roleManager.Roles.AsNoTracking()
            .Where(r => r.Name != null && adminRoleNames.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var adminUserIds = await _db.UserRoles.AsNoTracking()
            .Where(ur => adminRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);

        var adminsCount = await _userManager.Users.AsNoTracking()
            .CountAsync(u => u.OrganizationId == orgId && adminUserIds.Contains(u.Id), ct);

        var allowedDomainsRaw = _configuration["ManagerSecurity:AllowedEmailDomains"] ?? string.Empty;
        var allowedDomains = allowedDomainsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var mfaRequiredForAdmins = bool.TryParse(_configuration["ManagerSecurity:MfaRequiredForAdmins"], out var mfaAdmins) && mfaAdmins;
        var mfaRequiredForAll = bool.TryParse(_configuration["ManagerSecurity:MfaRequiredForAll"], out var mfaAll) && mfaAll;
        var invitationExpiresHours = int.TryParse(_configuration["ManagerSecurity:InvitationExpiryHours"], out var expH) ? expH : 48;

        return Ok(new ManagerBootstrapDto
        {
            Organization = new BootstrapOrganizationDto
            {
                Id = orgId,
                Name = _org.OrganizationName ?? string.Empty
            },
            SecuritySettings = new BootstrapSecuritySettingsDto
            {
                MfaRequiredForAdmins = mfaRequiredForAdmins,
                MfaRequiredForAll = mfaRequiredForAll,
                InvitationExpiresHours = invitationExpiresHours,
                AllowedEmailDomains = allowedDomains
            },
            PermissionsCatalogVersion = permissionsCatalogVersion,
            PermissionsCatalog = permissionsCatalog,
            MyEffectivePermissions = myPerms,
            MyRoles = myRoles,
            Kpis = new BootstrapKpisDto
            {
                UsersTotal = usersTotal,
                UsersActive = usersActive,
                InvitationsPending = invitationsPending,
                AdminsCount = adminsCount
            }
        });
    }

    [HttpGet("users")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(ManagerPagedResultDto<UserRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int take = 50,
        [FromQuery] int skip = 0,
        [FromQuery] string? query = null,
        [FromQuery] string? status = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? mfa = null,
        [FromQuery] string? sort = null,
        CancellationToken ct = default)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var userIdsInOrg = _db.UserOrganizations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .Select(x => x.UserId);

        var q = _userManager.Users.AsNoTracking().Where(u => userIdsInOrg.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var s = query.Trim().ToLower();
            q = q.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(s))
                || (u.FirstName != null && u.FirstName.ToLower().Contains(s))
                || (u.LastName != null && u.LastName.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim().ToLowerInvariant();

            if (st == "active") q = q.Where(u => u.Status == UserStatus.Active);
            else if (st == "inactive") q = q.Where(u => u.Status == UserStatus.Deactivated);
            else if (st == "suspended") q = q.Where(u => u.Status == UserStatus.Suspended);
            else if (st == "pending") q = q.Where(u => u.Status == UserStatus.PendingProvisioning || u.Status == UserStatus.ProvisioningFailed);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            var roleEntity = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == roleName, ct);
            if (roleEntity != null)
            {
                var userIdsInRole = _db.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.RoleId == roleEntity.Id)
                    .Select(ur => ur.UserId);

                q = q.Where(u => userIdsInRole.Contains(u.Id));
            }
        }

        if (mfa.HasValue)
        {
            q = q.Where(u => u.MfaEnabled == mfa.Value);
        }

        if (take <= 0) take = 50;
        if (take > 200) take = 200;
        if (skip < 0) skip = 0;

        var total = await q.CountAsync(ct);

        q = ApplySort(q, sort);

        var users = await q
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var items = new List<UserRowDto>();
        foreach (var u in users)
        {
            var rolesForUser = await _userRoleService.GetUserRolesAsync(u);

            var fullName = string.Join(' ', new[] { u.FirstName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

            items.Add(new UserRowDto
            {
                UserId = u.Id,
                FullName = string.IsNullOrWhiteSpace(fullName) ? (u.Email ?? string.Empty) : fullName,
                Email = u.Email ?? string.Empty,
                Status = MapUserStatus(u.Status),
                Roles = rolesForUser,
                Role = rolesForUser.Count > 0
                    ? new RoleRefDto
                    {
                        Key = MapRoleLookup(rolesForUser[0]).Key,
                        Label = MapRoleLookup(rolesForUser[0]).Label,
                        Type = MapRoleLookup(rolesForUser[0]).Type
                    }
                    : new RoleRefDto { Key = "Viewer", Label = "Viewer", Type = "system" },
                MfaEnabled = u.MfaEnabled,
                LastLoginUtc = u.LastLoginAtUtc,
                CreatedAtUtc = u.CreatedAtUtc
            });
        }

        return Ok(new ManagerPagedResultDto<UserRowDto>
        {
            Items = items,
            Total = total,
            Take = take,
            Skip = skip
        });
    }

    public sealed class ManagerUsersStatsDto
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Pending { get; set; }
        public int Mfa { get; set; }
    }

    [HttpGet("users/stats")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(ManagerUsersStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersStats(CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var userIdsInOrg = _db.UserOrganizations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .Select(x => x.UserId);

        var q = _userManager.Users.AsNoTracking().Where(u => userIdsInOrg.Contains(u.Id));

        var total = await q.CountAsync(ct);
        var active = await q.CountAsync(u => u.Status == UserStatus.Active, ct);
        var pending = await q.CountAsync(u => u.Status == UserStatus.PendingProvisioning || u.Status == UserStatus.ProvisioningFailed, ct);
        var mfaCount = await q.CountAsync(u => u.MfaEnabled, ct);

        return Ok(new ManagerUsersStatsDto
        {
            Total = total,
            Active = active,
            Pending = pending,
            Mfa = mfaCount
        });
    }

    public sealed class RolesLookupResponseDto
    {
        public List<RoleLookupItemDto> Items { get; set; } = new();
    }

    public sealed class RoleLookupItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    [HttpGet("roles/lookup")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(RolesLookupResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolesLookup(CancellationToken ct)
    {
        var roleNames = await _roleManager.Roles.AsNoTracking()
            .Where(r => r.Name != null)
            .OrderBy(r => r.Name)
            .Select(r => r.Name!)
            .ToListAsync(ct);

        var items = roleNames
            .Select(MapRoleLookup)
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Label)
            .ToList();

        return Ok(new RolesLookupResponseDto { Items = items });
    }

    private static RoleLookupItemDto MapRoleLookup(string roleName)
    {
        var sys = MapSystemRole(roleName);
        if (sys != null) return sys;

        return new RoleLookupItemDto
        {
            Key = $"Custom:{roleName}",
            Label = roleName,
            Type = "custom"
        };
    }

    private static RoleLookupItemDto? MapSystemRole(string roleName)
    {
        if (string.Equals(roleName, AuthGate.Auth.Domain.Constants.Roles.TenantOwner, StringComparison.OrdinalIgnoreCase))
            return new RoleLookupItemDto { Key = "OrgOwner", Label = "Org Owner", Type = "system" };
        if (string.Equals(roleName, AuthGate.Auth.Domain.Constants.Roles.TenantAdmin, StringComparison.OrdinalIgnoreCase))
            return new RoleLookupItemDto { Key = "OrgAdmin", Label = "Org Admin", Type = "system" };
        if (string.Equals(roleName, AuthGate.Auth.Domain.Constants.Roles.TenantManager, StringComparison.OrdinalIgnoreCase))
            return new RoleLookupItemDto { Key = "UserManager", Label = "User Manager", Type = "system" };
        if (string.Equals(roleName, AuthGate.Auth.Domain.Constants.Roles.Auditor, StringComparison.OrdinalIgnoreCase))
            return new RoleLookupItemDto { Key = "Auditor", Label = "Auditor", Type = "system" };
        if (string.Equals(roleName, AuthGate.Auth.Domain.Constants.Roles.ReadOnly, StringComparison.OrdinalIgnoreCase))
            return new RoleLookupItemDto { Key = "Viewer", Label = "Viewer", Type = "system" };
        return null;
    }

    public sealed class EffectivePermissionsResponseDto
    {
        public List<EffectivePermissionModuleDto> Items { get; set; } = new();
    }

    public sealed class EffectivePermissionModuleDto
    {
        public string Module { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    [HttpGet("users/{userId:guid}/permissions/effective")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(EffectivePermissionsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEffectivePermissions([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return NotFound();

        var roles = await _userRoleService.GetUserRolesAsync(user);
        var perms = await _userRoleService.GetUserPermissionsAsync(user);

        var byModule = perms
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .GroupBy(p => p.Split('.', 2)[0])
            .OrderBy(g => g.Key)
            .Select(g => new EffectivePermissionModuleDto
            {
                Module = g.Key,
                Source = roles.Count == 1 ? $"Rôle: {roles[0]}" : "Rôles",
                Permissions = g.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            })
            .ToList();

        return Ok(new EffectivePermissionsResponseDto { Items = byModule });
    }

    public sealed class UserSessionsResponseDto
    {
        public List<UserSessionDto> Items { get; set; } = new();
    }

    public sealed class UserSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime LastActive { get; set; }
        public bool Current { get; set; }
    }

    [HttpGet("users/{userId:guid}/sessions")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(UserSessionsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserSessions([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        return Ok(new UserSessionsResponseDto { Items = new List<UserSessionDto>() });
    }

    [HttpPost("users/{userId:guid}/mfa/reset")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetUserMfa([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;
        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        user.MfaEnabled = false;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    public sealed class ChangeUserRoleRequestDto
    {
        public string RoleKey { get; set; } = string.Empty;
    }

    [HttpPost("users/{userId:guid}/role")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeUserRole([FromRoute] Guid userId, [FromBody] ChangeUserRoleRequestDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.RoleKey))
            return BadRequest(new { error = "roleKey is required" });

        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        var roleName = dto.RoleKey.Trim();
        var roleEntity = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name != null && r.Name == roleName, ct);
        if (roleEntity == null)
            return BadRequest(new { error = "roleKey is invalid" });

        var currentRoles = await _userRoleService.GetUserRolesAsync(user);
        var adminRoleNames = new[] { AuthGate.Auth.Domain.Constants.Roles.TenantOwner, AuthGate.Auth.Domain.Constants.Roles.TenantAdmin };
        var isCurrentlyAdmin = currentRoles.Any(r => adminRoleNames.Contains(r));
        var isTargetAdmin = adminRoleNames.Contains(roleName);

        if (isCurrentlyAdmin && !isTargetAdmin)
        {
            var adminRoleIds = await _roleManager.Roles.AsNoTracking()
                .Where(r => r.Name != null && adminRoleNames.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync(ct);

            var adminUserIds = await _db.UserRoles.AsNoTracking()
                .Where(ur => adminRoleIds.Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync(ct);

            var adminCountInOrg = await _db.UserOrganizations.AsNoTracking()
                .Where(x => x.OrganizationId == orgId && adminUserIds.Contains(x.UserId))
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync(ct);

            if (adminCountInOrg <= 1)
                return Conflict(new { error = "Cannot remove the last admin from the organization" });
        }

        var userRoleLinks = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(ct);

        if (userRoleLinks.Count > 0)
        {
            _db.UserRoles.RemoveRange(userRoleLinks);
        }

        _db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleEntity.Id });
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("users/{userId:guid}/invitation/resend")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendInvitation([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return NotFound();

        var normalizedEmail = user.Email.Trim().ToLowerInvariant();

        var inv = await _db.ManagerInvitations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId
                        && x.Email == normalizedEmail
                        && x.Status == ManagerInvitationStatus.Pending
                        && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (inv == null)
            return Conflict(new { error = "No active invitation found for this user" });

        var frontendUrl = _configuration["ManagerFrontend:BaseUrl"]
                          ?? _configuration["Frontend:BaseUrl"]
                          ?? string.Empty;

        var link = string.IsNullOrWhiteSpace(frontendUrl)
            ? inv.Id.ToString("D")
            : $"{frontendUrl.TrimEnd('/')}/manager-invitation/{inv.Id:D}";

        var subject = "Accès au portail Manager";

        var html = $$"""
<h2>{{subject}}</h2>
<p>Bonjour,</p>
<p>Votre invitation au portail <strong>LocaGuest Manager</strong> a été renvoyée.</p>
<p><a href=\"{{link}}\">Ouvrir</a></p>
<p style=\"font-size: 14px; color: #6b7280;\">Cette invitation expire le <strong>{{inv.ExpiresAtUtc:dd/MM/yyyy à HH:mm}} UTC</strong></p>
""";

        await _emailing.QueueHtmlAsync(
            toEmail: user.Email.Trim(),
            subject: subject,
            htmlContent: html,
            textContent: null,
            attachments: null,
            tags: EmailUseCaseTags.AccessInviteUser,
            cancellationToken: ct);

        return NoContent();
    }

    [HttpPost("users/{userId:guid}/sessions/revoke-all")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeAllSessions([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;
        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        await _db.RefreshTokens
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return NoContent();
    }

    [HttpPost("users/{userId:guid}/suspend")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendUser([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;
        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return NotFound();

        user.Status = UserStatus.Suspended;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

    [HttpDelete("users/{userId:guid}/membership")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeMembership([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var deleted = await _db.UserOrganizations
            .Where(x => x.OrganizationId == orgId && x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
            return NotFound();

        return NoContent();
    }

    [HttpGet("users/{userId:guid}")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserDetails([FromRoute] Guid userId, CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var inOrg = await _db.UserOrganizations.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == orgId && x.UserId == userId, ct);
        if (!inOrg)
            return NotFound();

        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user == null)
            return NotFound();

        var roles = await _userRoleService.GetUserRolesAsync(user);
        var perms = await _userRoleService.GetUserPermissionsAsync(user);

        var fullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        // MVP: sessions/audit summaries will be enriched later.
        var dto = new UserDetailsDto
        {
            UserId = user.Id,
            FullName = string.IsNullOrWhiteSpace(fullName) ? (user.Email ?? string.Empty) : fullName,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber,
            Status = MapUserStatus(user.Status),
            Roles = roles,
            Role = roles.Count > 0
                ? new RoleRefDto
                {
                    Key = MapRoleLookup(roles[0]).Key,
                    Label = MapRoleLookup(roles[0]).Label,
                    Type = MapRoleLookup(roles[0]).Type
                }
                : new RoleRefDto { Key = "Viewer", Label = "Viewer", Type = "system" },
            PermissionsEffective = perms,
            MfaEnabled = user.MfaEnabled,
            CreatedAtUtc = user.CreatedAtUtc,
            LastLoginUtc = user.LastLoginAtUtc,
            Security = new UserSecurityDetailsDto
            {
                PasswordLastChangedAtUtc = user.PasswordLastChangedAtUtc,
                MustChangePassword = user.MustChangePassword,
                LockedUntilUtc = user.LockoutEndUtc,
                FailedLoginCount = user.FailedLoginAttempts
            },
            Sessions = new UserSessionsSummaryDto
            {
                ActiveSessions = 0,
                LastIp = null,
                LastUserAgent = null
            },
            AuditSummary = new UserAuditSummaryDto
            {
                LastRoleChangeUtc = null,
                LastDisableUtc = null
            }
        };

        return Ok(dto);
    }

    private static IQueryable<User> ApplySort(IQueryable<User> q, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return q.OrderByDescending(u => u.LastLoginAtUtc ?? DateTime.MinValue)
                .ThenBy(u => u.Email);
        }

        var s = sort.Trim().ToLowerInvariant();
        var desc = s.Contains(":desc") || s.EndsWith(" desc");
        var key = s.Replace(":desc", "").Replace(":asc", "").Replace(" desc", "").Replace(" asc", "").Trim();

        if (key == "lastlogin") return desc ? q.OrderByDescending(u => u.LastLoginAtUtc) : q.OrderBy(u => u.LastLoginAtUtc);
        if (key == "email") return desc ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email);
        if (key == "name")
        {
            return desc
                ? q.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
                : q.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);
        }

        return q.OrderByDescending(u => u.LastLoginAtUtc ?? DateTime.MinValue).ThenBy(u => u.Email);
    }

    private static string MapUserStatus(UserStatus status)
    {
        return status switch
        {
            UserStatus.Active => "active",
            UserStatus.PendingProvisioning => "pending",
            UserStatus.ProvisioningFailed => "pending",
            UserStatus.Suspended => "suspended",
            UserStatus.Deactivated => "inactive",
            _ => "active"
        };
    }

    [HttpGet("roles")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken ct)
    {
        var roles = await _roleManager.Roles.AsNoTracking().ToListAsync(ct);
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var userCount = await _db.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == role.Id, ct);
            var permissionCount = await _db.RolePermissions.AsNoTracking().CountAsync(rp => rp.RoleId == role.Id, ct);

            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                CreatedAtUtc = role.CreatedAtUtc,
                UserCount = userCount,
                PermissionCount = permissionCount
            });
        }

        return Ok(roleDtos);
    }

    [HttpGet("permissions")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions(CancellationToken ct)
    {
        var permissions = await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Code)
            .ToListAsync(ct);

        var permissionDtos = permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Code = p.Code,
            DisplayName = p.DisplayName,
            Description = p.Description,
            Category = p.Category,
            IsActive = p.IsActive
        }).ToList();

        return Ok(permissionDtos);
    }

    [HttpGet("settings/security")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(ManagerSecuritySettingsDto), StatusCodes.Status200OK)]
    public IActionResult GetSecuritySettings()
    {
        var mfaRequired = bool.TryParse(_configuration["ManagerSecurity:MfaRequired"], out var b) && b;
        var minPasswordLength = int.TryParse(_configuration["ManagerSecurity:MinPasswordLength"], out var i) ? i : 12;
        var invitationsEnabled = !bool.TryParse(_configuration["ManagerSecurity:InvitationsDisabled"], out var invDisabled) || !invDisabled;
        var invitationExpiryHours = int.TryParse(_configuration["ManagerSecurity:InvitationExpiryHours"], out var h) ? h : 48;

        return Ok(new ManagerSecuritySettingsDto
        {
            MfaRequired = mfaRequired,
            MinPasswordLength = minPasswordLength,
            InvitationsEnabled = invitationsEnabled,
            InvitationExpiryHours = invitationExpiryHours
        });
    }

    [HttpGet("dashboard/summary")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [ProducesResponseType(typeof(ManagerDashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        var usersQuery = _userManager.Users.AsNoTracking().Where(u => u.OrganizationId == orgId);
        var totalUsers = await usersQuery.CountAsync(ct);
        var activeUsers = await usersQuery.CountAsync(u => u.Status == UserStatus.Active, ct);

        var rolesCount = await _roleManager.Roles.AsNoTracking().CountAsync(ct);
        var permissionsCount = await _db.Permissions.AsNoTracking().CountAsync(p => p.IsActive, ct);

        var nowUtc = DateTime.UtcNow;
        var pendingInvitations = await _db.ManagerInvitations.AsNoTracking()
            .CountAsync(x => x.OrganizationId == orgId && x.Status == ManagerInvitationStatus.Pending && x.ExpiresAtUtc > nowUtc, ct);

        return Ok(new ManagerDashboardSummaryDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            RolesCount = rolesCount,
            PermissionsCount = permissionsCount,
            PendingInvitations = pendingInvitations
        });
    }

    [HttpGet("dashboard/recent-activities")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [HasPermission(Permissions.AuditLogsRead)]
    [ProducesResponseType(typeof(ManagerDashboardRecentActivitiesResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActivities([FromQuery] int take = 10, CancellationToken ct = default)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        if (take <= 0) take = 10;
        if (take > 50) take = 50;

        // Read last audit logs and map them to stable dashboard activities.
        // We filter using metadata when possible (org_id/organization_id). If metadata is missing, we keep the entry
        // only if it is related to a user in the current org.

        var orgUserIds = await _db.UserOrganizations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .Select(x => x.UserId)
            .ToListAsync(ct);

        var candidates = await _audit.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(200)
            .Select(x => new { x.Id, x.UserId, x.Action, x.Description, x.IsSuccess, x.Metadata, x.CreatedAtUtc })
            .ToListAsync(ct);

        bool MatchesOrgByMetadata(string? metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata)) return false;
            try
            {
                using var doc = JsonDocument.Parse(metadata);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;

                if (doc.RootElement.TryGetProperty("org_id", out var orgProp)
                    || doc.RootElement.TryGetProperty("organization_id", out orgProp)
                    || doc.RootElement.TryGetProperty("organizationId", out orgProp))
                {
                    if (orgProp.ValueKind == JsonValueKind.String
                        && Guid.TryParse(orgProp.GetString(), out var g)
                        && g == orgId)
                        return true;

                    if (orgProp.ValueKind == JsonValueKind.Number
                        && orgProp.TryGetInt64(out var n)
                        && Guid.TryParse(n.ToString(), out var g2)
                        && g2 == orgId)
                        return true;
                }

                // Switch org event uses fromOrgId/toOrgId
                if (doc.RootElement.TryGetProperty("toOrgId", out var toOrg)
                    && toOrg.ValueKind == JsonValueKind.String
                    && Guid.TryParse(toOrg.GetString(), out var toOrgId)
                    && toOrgId == orgId)
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        var filtered = candidates
            .Where(x => MatchesOrgByMetadata(x.Metadata) || (x.UserId.HasValue && orgUserIds.Contains(x.UserId.Value)))
            .Take(take)
            .ToList();

        var actorIds = filtered
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId!.Value)
            .Distinct()
            .ToList();

        var actors = await _userManager.Users
            .AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName
            })
            .ToListAsync(ct);

        string ActorName(Guid? userId)
        {
            if (!userId.HasValue) return "Système";
            var u = actors.FirstOrDefault(a => a.Id == userId.Value);
            if (u == null) return "Utilisateur";
            var full = string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(full) ? (u.Email ?? "Utilisateur") : full;
        }

        var items = filtered.Select(x => new ManagerDashboardRecentActivityDto
        {
            Id = x.Id.ToString("D"),
            Action = string.IsNullOrWhiteSpace(x.Description) ? x.Action.ToString() : x.Description!,
            Actor = ActorName(x.UserId),
            Target = string.Empty,
            TimeUtc = x.CreatedAtUtc,
            Type = x.IsSuccess ? "success" : "warning"
        }).ToList();

        return Ok(new ManagerDashboardRecentActivitiesResponseDto { Items = items });
    }

    [HttpGet("dashboard/users-without-mfa")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [HasPermission("users.read")]
    [ProducesResponseType(typeof(ManagerDashboardUsersWithoutMfaResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersWithoutMfa([FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        var userIdsInOrg = _db.UserOrganizations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .Select(x => x.UserId);

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => userIdsInOrg.Contains(u.Id) && !u.MfaEnabled)
            .OrderByDescending(u => u.LastLoginAtUtc ?? DateTime.MinValue)
            .Take(take)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.LastLoginAtUtc
            })
            .ToListAsync(ct);

        var items = users.Select(u =>
        {
            var fullName = string.Join(' ', new[] { u.FirstName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(fullName)) fullName = u.Email ?? string.Empty;
            return new ManagerDashboardUserWithoutMfaDto
            {
                Id = u.Id,
                Name = fullName,
                Email = u.Email ?? string.Empty,
                LastLoginUtc = u.LastLoginAtUtc
            };
        }).ToList();

        return Ok(new ManagerDashboardUsersWithoutMfaResponseDto { Items = items });
    }

    [HttpGet("dashboard/admins-full-access")]
    [Authorize]
    [Authorize(Policy = "NoPasswordChangeRequired")]
    [Authorize(Policy = "TenantContextRequired")]
    [HasPermission("users.read")]
    [ProducesResponseType(typeof(ManagerDashboardAdminsFullAccessResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminsFullAccess([FromQuery] int take = 50, CancellationToken ct = default)
    {
        if (!_org.OrganizationId.HasValue)
            return Forbid();

        var orgId = _org.OrganizationId.Value;

        if (take <= 0) take = 50;
        if (take > 200) take = 200;

        var adminRoleNames = new[] { AuthGate.Auth.Domain.Constants.Roles.TenantOwner, AuthGate.Auth.Domain.Constants.Roles.TenantAdmin };

        var adminRoleIds = await _roleManager.Roles
            .AsNoTracking()
            .Where(r => r.Name != null && adminRoleNames.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync(ct);

        var adminUserIds = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => adminRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(ct);

        var usersInOrg = _db.UserOrganizations
            .AsNoTracking()
            .Where(x => x.OrganizationId == orgId)
            .Select(x => x.UserId);

        var admins = await _userManager.Users
            .AsNoTracking()
            .Where(u => usersInOrg.Contains(u.Id) && adminUserIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .Take(take)
            .ToListAsync(ct);

        var items = new List<ManagerDashboardAdminFullAccessDto>();
        foreach (var u in admins)
        {
            var roles = await _userRoleService.GetUserRolesAsync(u);
            var adminRoleLabel = roles.FirstOrDefault(r => adminRoleNames.Contains(r)) ?? roles.FirstOrDefault() ?? "Admin";

            var fullName = string.Join(' ', new[] { u.FirstName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrWhiteSpace(fullName)) fullName = u.Email ?? string.Empty;

            items.Add(new ManagerDashboardAdminFullAccessDto
            {
                Id = u.Id,
                Name = fullName,
                Email = u.Email ?? string.Empty,
                Role = adminRoleLabel,
                SinceUtc = u.CreatedAtUtc
            });
        }

        return Ok(new ManagerDashboardAdminsFullAccessResponseDto { Items = items });
    }
}
