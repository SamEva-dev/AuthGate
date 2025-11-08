using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents the MFA (TOTP) secret for a user
/// </summary>
public class MfaSecret : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user this MFA secret belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the encrypted secret key used for TOTP generation
    /// </summary>
    public required string EncryptedSecret { get; set; }

    /// <summary>
    /// Gets or sets whether this MFA secret has been verified by the user
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the date and time when MFA was verified (UTC)
    /// </summary>
    public DateTime? VerifiedAtUtc { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user this MFA secret belongs to
    /// </summary>
    public virtual User User { get; set; } = null!;
}
