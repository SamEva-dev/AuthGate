namespace AuthGate.Auth.Application.Common.Clients.Models;

public sealed record ProvisionOrganizationRequest
{
    public string OrganizationName { get; init; } = string.Empty;
    public string OrganizationEmail { get; init; } = string.Empty;
    public string? OrganizationPhone { get; init; }

    public string OwnerUserId { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
}

public sealed record ProvisionOrganizationResponse
{
    public Guid OrganizationId { get; init; }
    public int Number { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
