using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a one-time token used for password reset requests
/// </summary>
public class PasswordResetToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the ID of the user this token belongs to
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the actual token value
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Gets or sets whether this token has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this token expires (UTC)
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this token was used (UTC)
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the reset was requested
    /// </summary>
    public string? RequestedFromIp { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user this token belongs to
    /// </summary>
    public virtual User User { get; set; } = null!;
}
