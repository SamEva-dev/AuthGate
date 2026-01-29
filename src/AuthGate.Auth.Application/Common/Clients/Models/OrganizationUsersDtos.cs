namespace AuthGate.Auth.Application.Common.Clients.Models;

public sealed record LocaGuestOrganizationUsersPagedResultDto
{
    public List<LocaGuestOrganizationUserDto> Items { get; init; } = new();
    public int Total { get; init; }
    public int Take { get; init; }
    public int Skip { get; init; }
}

public sealed record LocaGuestOrganizationUserDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime InvitedAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
}

public sealed record LocaGuestOrganizationUsersStatsDto
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Pending { get; init; }
}
