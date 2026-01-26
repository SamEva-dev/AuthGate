using AuthGate.Auth.Application.DTOs.Manager;
using AuthGate.Auth.Application.DTOs.Permissions;
using AuthGate.Auth.Application.DTOs.Roles;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/manager")]
[Authorize(Policy = "ManagerAppRequired")]
public class ManagerController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IUserRoleService _userRoleService;
    private readonly IOrganizationContext _org;
    private readonly IConfiguration _configuration;

    public ManagerController(
        AuthDbContext db,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IUserRoleService userRoleService,
        IOrganizationContext org,
        IConfiguration configuration)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _userRoleService = userRoleService;
        _org = org;
        _configuration = configuration;
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
            else if (st == "disabled") q = q.Where(u => u.Status == UserStatus.Deactivated);
            else if (st == "pending") q = q.Where(u => u.Status == UserStatus.PendingProvisioning);
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
            Status = MapUserStatus(user.Status),
            Roles = roles,
            PermissionsEffective = perms,
            MfaEnabled = user.MfaEnabled,
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
            UserStatus.Active => "Active",
            UserStatus.PendingProvisioning => "Pending",
            UserStatus.Deactivated => "Disabled",
            UserStatus.Suspended => "Disabled",
            UserStatus.ProvisioningFailed => "Pending",
            _ => "Active"
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
}
