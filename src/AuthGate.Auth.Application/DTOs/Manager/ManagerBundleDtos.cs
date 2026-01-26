namespace AuthGate.Auth.Application.DTOs.Manager;

using AuthGate.Auth.Application.DTOs.Permissions;
using AuthGate.Auth.Application.DTOs.Roles;

public sealed class PagedResultDto<TItem>
{
    public List<TItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Take { get; set; }
    public int Skip { get; set; }
}

public sealed class GetManagerUsersRequestDto
{
    public int Take { get; set; } = 50;
    public int Skip { get; set; }
    public string? Query { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
}

public sealed class ManagerUserListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool MfaEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public List<string> Roles { get; set; } = new();
}

public sealed class GetManagerUsersResponseDto
{
    public PagedResultDto<ManagerUserListItemDto> Result { get; set; } = new();
}

public sealed class ManagerSecuritySettingsDto
{
    public bool MfaRequired { get; set; }
    public int MinPasswordLength { get; set; }
    public bool InvitationsEnabled { get; set; }
    public int InvitationExpiryHours { get; set; }
}

public sealed class ManagerDashboardSummaryDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int RolesCount { get; set; }
    public int PermissionsCount { get; set; }
    public int PendingInvitations { get; set; }
}

public sealed class ManagerBundleDto
{
    public PagedResultDto<ManagerUserListItemDto> Users { get; set; } = new();
    public List<RoleDto> Roles { get; set; } = new();
    public List<PermissionDto> Permissions { get; set; } = new();
    public ManagerSecuritySettingsDto SecuritySettings { get; set; } = new();
    public ManagerDashboardSummaryDto DashboardSummary { get; set; } = new();
}
