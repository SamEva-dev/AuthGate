using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a refresh token used for JWT token rotation
/// </summary>
public class RefreshToken : BaseEntity
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
    /// Gets or sets the JWT ID (jti) that this refresh token is associated with
    /// </summary>
    public required string JwtId { get; set; }

    /// <summary>
    /// Gets or sets whether this token has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets whether this token has been revoked
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this token expires (UTC)
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the reason for revocation (if revoked)
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this token was revoked (UTC)
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the token was created
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Gets or sets the ID of the token that replaced this one (for rotation)
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user this token belongs to
    /// </summary>
    public virtual User User { get; set; } = null!;
}
