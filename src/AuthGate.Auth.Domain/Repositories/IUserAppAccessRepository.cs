using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

public interface IUserAppAccessRepository
{
    Task<bool> HasAccessAsync(Guid userId, string appId, CancellationToken cancellationToken = default);

    Task AddAsync(UserAppAccess access, CancellationToken cancellationToken = default);
}
