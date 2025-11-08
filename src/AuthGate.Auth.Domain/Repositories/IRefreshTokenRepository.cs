using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for RefreshToken entity operations
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by its value
    /// </summary>
    /// <param name="token">The token value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of refresh tokens</returns>
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to update</param>
    void Update(RefreshToken refreshToken);

    /// <summary>
    /// Revokes all active tokens for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens deleted</returns>
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
}
