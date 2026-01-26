using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

public interface IUserOrganizationRepository
{
    Task<IEnumerable<UserOrganization>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default);

    Task AddAsync(UserOrganization userOrganization, CancellationToken cancellationToken = default);

    void Remove(UserOrganization userOrganization);
}
