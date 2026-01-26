namespace AuthGate.Auth.Application.Common.Clients.Models;

public sealed record LocaGuestOrganizationDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed record LocaGuestOrganizationListItemDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
