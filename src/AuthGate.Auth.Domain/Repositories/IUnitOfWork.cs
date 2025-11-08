namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Unit of Work interface for managing database transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the user repository
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the role repository
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Gets the permission repository
    /// </summary>
    IPermissionRepository Permissions { get; }

    /// <summary>
    /// Gets the refresh token repository
    /// </summary>
    IRefreshTokenRepository RefreshTokens { get; }

    /// <summary>
    /// Gets the audit log repository
    /// </summary>
    IAuditLogRepository AuditLogs { get; }

    /// <summary>
    /// Saves all changes made in this unit of work to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
