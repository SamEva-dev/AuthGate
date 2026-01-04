using AuthGate.Auth.Application.Common.Clients.Models;

namespace AuthGate.Auth.Application.Common.Clients;

public interface ILocaGuestInvitationProvisioningClient
{
    Task<ConsumeInvitationResponse?> ConsumeInvitationAsync(
        ConsumeInvitationRequest request,
        CancellationToken ct = default);
}
