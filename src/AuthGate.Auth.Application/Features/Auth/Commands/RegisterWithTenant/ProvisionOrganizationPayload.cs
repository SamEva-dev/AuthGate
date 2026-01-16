namespace AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;

/// <summary>
/// Payload for the ProvisionOrganization outbox message
/// </summary>
public record ProvisionOrganizationPayload
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
