using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a recovery code that can be used to bypass MFA if the user loses access to their authenticator
/// </summary>
public class RecoveryCode : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user this recovery code belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the hashed recovery code value
    /// </summary>
    public required string CodeHash { get; set; }

    /// <summary>
    /// Gets or sets whether this recovery code has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this code was used (UTC)
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user this recovery code belongs to
    /// </summary>
    public virtual User User { get; set; } = null!;
}
