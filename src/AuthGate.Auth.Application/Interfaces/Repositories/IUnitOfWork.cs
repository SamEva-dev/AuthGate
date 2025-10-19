namespace AuthGate.Auth.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    IAuthRepository Auth { get; }

    IPermissionRepository Permissions { get; }

    IRoleRepository Roles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}