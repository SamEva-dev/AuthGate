namespace AuthGate.Auth.Application.DTOs.Manager;

public sealed class ManagerPagedResultDto<TItem>
{
    public List<TItem> Items { get; set; } = new();
    public int Total { get; set; }
    public int Take { get; set; }
    public int Skip { get; set; }
}

public sealed class RoleRefDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public sealed class UserRowDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public RoleRefDto Role { get; set; } = new();
    public bool MfaEnabled { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class UserDetailsDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public RoleRefDto Role { get; set; } = new();
    public List<string> PermissionsEffective { get; set; } = new();
    public bool MfaEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginUtc { get; set; }

    public UserSecurityDetailsDto Security { get; set; } = new();
    public UserSessionsSummaryDto Sessions { get; set; } = new();
    public UserAuditSummaryDto AuditSummary { get; set; } = new();
}

public sealed class UserSecurityDetailsDto
{
    public DateTime? PasswordLastChangedAtUtc { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public int FailedLoginCount { get; set; }
}

public sealed class UserSessionsSummaryDto
{
    public int ActiveSessions { get; set; }
    public string? LastIp { get; set; }
    public string? LastUserAgent { get; set; }
}

public sealed class UserAuditSummaryDto
{
    public DateTime? LastRoleChangeUtc { get; set; }
    public DateTime? LastDisableUtc { get; set; }
}
