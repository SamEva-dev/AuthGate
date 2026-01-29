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

    Task<IReadOnlyList<LocaGuestOrganizationSessionDto>> GetOrganizationSessionsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<LocaGuestOrganizationUsersPagedResultDto?> GetOrganizationUsersAsync(
        Guid organizationId,
        int take,
        int skip,
        string? query,
        string? status,
        CancellationToken ct = default);

    Task<LocaGuestOrganizationUsersStatsDto?> GetOrganizationUsersStatsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<LocaGuestPagedResultDto<LocaGuestAuditLogDto>?> GetAuditLogsAsync(
        int page,
        int pageSize,
        Guid? userId,
        Guid? organizationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);

    Task<bool> HardDeleteOrganizationAsync(Guid organizationId, CancellationToken ct = default);
}
