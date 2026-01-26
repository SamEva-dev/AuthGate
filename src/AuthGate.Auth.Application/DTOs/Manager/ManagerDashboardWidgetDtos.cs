namespace AuthGate.Auth.Application.DTOs.Manager;

public sealed class ManagerDashboardRecentActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public DateTime TimeUtc { get; set; }
    public string Type { get; set; } = "info";
}

public sealed class ManagerDashboardRecentActivitiesResponseDto
{
    public List<ManagerDashboardRecentActivityDto> Items { get; set; } = new();
}

public sealed class ManagerDashboardUserWithoutMfaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? LastLoginUtc { get; set; }
}

public sealed class ManagerDashboardUsersWithoutMfaResponseDto
{
    public List<ManagerDashboardUserWithoutMfaDto> Items { get; set; } = new();
}

public sealed class ManagerDashboardAdminFullAccessDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime? SinceUtc { get; set; }
}

public sealed class ManagerDashboardAdminsFullAccessResponseDto
{
    public List<ManagerDashboardAdminFullAccessDto> Items { get; set; } = new();
}

public sealed class DashboardInviteUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string RoleKey { get; set; } = string.Empty;
}
