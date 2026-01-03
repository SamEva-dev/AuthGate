using AuthGate.Auth.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a user in the authentication system, extending IdentityUser for hybrid approach
/// </summary>
public class User : IdentityUser<Guid>, IAuditableEntity
{
    /// <summary>
    /// Gets or sets the user's first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Gets or sets whether the account is locked out
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Gets or sets the lockout end date in UTC
    /// </summary>
    public DateTime? LockoutEndUtc { get; set; }

    /// <summary>
    /// Gets or sets the last successful login date in UTC
    /// </summary>
    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether MFA is enabled
    /// </summary>
    public bool MfaEnabled { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant isolation in LocaGuest
    /// Required for all users except SuperAdmin
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <inheritdoc/>
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc/>
    public Guid? UpdatedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user's refresh tokens
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    /// <summary>
    /// Gets or sets the user's MFA secret
    /// </summary>
    public virtual MfaSecret? MfaSecret { get; set; }

    /// <summary>
    /// Gets or sets the user's recovery codes
    /// </summary>
    public virtual ICollection<RecoveryCode> RecoveryCodes { get; set; } = new List<RecoveryCode>();

    /// <summary>
    /// Gets or sets the user's password reset tokens
    /// </summary>
    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
