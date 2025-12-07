using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for TrustedDevice entity operations
/// </summary>
public interface ITrustedDeviceRepository
{
    /// <summary>
    /// Gets a trusted device by user ID and device fingerprint
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="deviceFingerprint">The device fingerprint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The trusted device if found and valid, null otherwise</returns>
    Task<TrustedDevice?> GetByUserAndFingerprintAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all trusted devices for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of trusted devices</returns>
    Task<IEnumerable<TrustedDevice>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new trusted device
    /// </summary>
    /// <param name="trustedDevice">The trusted device to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(TrustedDevice trustedDevice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing trusted device
    /// </summary>
    /// <param name="trustedDevice">The trusted device to update</param>
    void Update(TrustedDevice trustedDevice);

    /// <summary>
    /// Revokes a trusted device by ID
    /// </summary>
    /// <param name="id">The trusted device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all trusted devices for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeAllUserDevicesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired trusted devices
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of devices deleted</returns>
    Task<int> DeleteExpiredDevicesAsync(CancellationToken cancellationToken = default);
}
