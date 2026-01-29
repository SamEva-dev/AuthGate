using System;

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

public sealed record LocaGuestOrganizationSessionDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string Browser { get; init; } = string.Empty;
    public string Os { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivityAt { get; init; }
}
