using AuthGate.Auth.Application.Common.Clients.Models;

namespace AuthGate.Auth.Application.Common.Clients;

public interface ILocaGuestProvisioningClient
{
    Task<ProvisionOrganizationResponse?> ProvisionOrganizationAsync(
        ProvisionOrganizationRequest request,
        CancellationToken ct = default);

    Task<LocaGuestOrganizationDetailsDto?> GetOrganizationByIdAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<IReadOnlyList<LocaGuestOrganizationListItemDto>> GetOrganizationsAsync(
        CancellationToken ct = default);

    Task<bool> HardDeleteOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}
