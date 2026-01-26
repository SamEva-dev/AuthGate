using AuthGate.Auth.Domain.Common;
using AuthGate.Auth.Domain.Enums;
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
    /// Gets or sets the provisioning status of the user
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

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

    public bool MustChangePassword { get; set; }

    public DateTime? MustChangePasswordBeforeUtc { get; set; }

    public DateTime? PasswordLastChangedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the external OAuth provider (google, facebook, etc.)
    /// </summary>
    public string? ExternalProvider { get; set; }

    /// <summary>
    /// Gets or sets the user's ID from the external OAuth provider
    /// </summary>
    public string? ExternalProviderId { get; set; }

    /// <summary>
    /// Gets or sets the user's profile picture URL (from OAuth or uploaded)
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the date when the account was deactivated (soft delete)
    /// </summary>
    public DateTime? DeactivatedAtUtc { get; set; }

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

    public virtual ICollection<UserOrganization> UserOrganizations { get; set; } = new List<UserOrganization>();
}
