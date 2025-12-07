using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a trusted device for a user that can skip 2FA verification
/// </summary>
public class TrustedDevice : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user this device belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the device fingerprint (hash of User-Agent + IP or more sophisticated)
    /// </summary>
    public required string DeviceFingerprint { get; set; }

    /// <summary>
    /// Gets or sets the friendly device name (e.g., "Chrome on Windows", "Safari on iPhone")
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which this device was trusted
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the User-Agent string of the trusted device
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this device expires as trusted (UTC)
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this device was last used (UTC)
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether this device trust has been revoked
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this device trust was revoked (UTC)
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user this trusted device belongs to
    /// </summary>
    public virtual User User { get; set; } = null!;

    // Business logic methods

    /// <summary>
    /// Checks if this trusted device is still valid (not expired and not revoked)
    /// </summary>
    public bool IsValid()
    {
        return !IsRevoked && ExpiresAtUtc > DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the last used timestamp
    /// </summary>
    public void UpdateLastUsed()
    {
        LastUsedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes this trusted device
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
        RevokedAtUtc = DateTime.UtcNow;
    }
}
