namespace AuthGate.Auth.Application.Common.Clients.Models;

public sealed class ConsumeInvitationRequest
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
}

public sealed class ConsumeInvitationResponse
{
    public Guid OrganizationId { get; set; }
    public Guid TeamMemberId { get; set; }
    public string Role { get; set; } = string.Empty;
}
