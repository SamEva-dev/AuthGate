namespace AuthGate.Auth.Domain.Enums;

/// <summary>
/// Enumeration of audit actions for tracking user activities
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// User logged in successfully
    /// </summary>
    Login,

    /// <summary>
    /// User login attempt failed
    /// </summary>
    LoginFailed,

    /// <summary>
    /// User logged out
    /// </summary>
    Logout,

    /// <summary>
    /// User refreshed authentication token
    /// </summary>
    TokenRefreshed,

    /// <summary>
    /// User enabled MFA
    /// </summary>
    MfaEnabled,

    /// <summary>
    /// User disabled MFA
    /// </summary>
    MfaDisabled,

    /// <summary>
    /// User verified MFA code successfully
    /// </summary>
    MfaVerified,

    /// <summary>
    /// User MFA verification failed
    /// </summary>
    MfaVerificationFailed,

    /// <summary>
    /// User requested password reset
    /// </summary>
    PasswordResetRequested,

    /// <summary>
    /// User reset password successfully
    /// </summary>
    PasswordReset,

    /// <summary>
    /// User password was changed
    /// </summary>
    PasswordChanged,

    /// <summary>
    /// User was created
    /// </summary>
    UserCreated,

    /// <summary>
    /// User was updated
    /// </summary>
    UserUpdated,

    /// <summary>
    /// User was deleted
    /// </summary>
    UserDeleted,

    /// <summary>
    /// Role was assigned to user
    /// </summary>
    RoleAssigned,

    /// <summary>
    /// Role was removed from user
    /// </summary>
    RoleRemoved,

    /// <summary>
    /// Permission was assigned to role
    /// </summary>
    PermissionAssigned,

    /// <summary>
    /// Permission was removed from role
    /// </summary>
    PermissionRemoved
}
