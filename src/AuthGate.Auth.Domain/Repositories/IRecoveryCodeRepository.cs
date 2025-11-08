using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for RecoveryCode entity operations
/// </summary>
public interface IRecoveryCodeRepository
{
    /// <summary>
    /// Gets all recovery codes for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of recovery codes</returns>
    Task<IEnumerable<RecoveryCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unused recovery codes for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of unused recovery codes</returns>
    Task<IEnumerable<RecoveryCode>> GetUnusedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new recovery code
    /// </summary>
    /// <param name="recoveryCode">The recovery code to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(RecoveryCode recoveryCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple recovery codes
    /// </summary>
    /// <param name="recoveryCodes">The recovery codes to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddRangeAsync(IEnumerable<RecoveryCode> recoveryCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing recovery code
    /// </summary>
    /// <param name="recoveryCode">The recovery code to update</param>
    void Update(RecoveryCode recoveryCode);

    /// <summary>
    /// Deletes all recovery codes for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
