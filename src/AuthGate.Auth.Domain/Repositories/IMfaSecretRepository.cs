using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for MfaSecret entity operations
/// </summary>
public interface IMfaSecretRepository
{
    /// <summary>
    /// Gets the MFA secret for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA secret if found, null otherwise</returns>
    Task<MfaSecret?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the verified MFA secret for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The verified MFA secret if found, null otherwise</returns>
    Task<MfaSecret?> GetVerifiedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new MFA secret
    /// </summary>
    /// <param name="mfaSecret">The MFA secret to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(MfaSecret mfaSecret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing MFA secret
    /// </summary>
    /// <param name="mfaSecret">The MFA secret to update</param>
    void Update(MfaSecret mfaSecret);

    /// <summary>
    /// Deletes MFA secrets for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
