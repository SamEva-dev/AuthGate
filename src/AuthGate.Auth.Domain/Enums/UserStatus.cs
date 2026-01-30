namespace AuthGate.Auth.Domain.Enums;

/// <summary>
/// Represents the provisioning status of a user
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User is pending email confirmation
    /// </summary>
    PendingEmailConfirmation = -1,

    /// <summary>
    /// User is pending organization provisioning (registration in progress)
    /// </summary>
    PendingProvisioning = 0,

    /// <summary>
    /// User is fully active with organization provisioned
    /// </summary>
    Active = 1,

    /// <summary>
    /// Provisioning failed and needs retry or manual intervention
    /// </summary>
    ProvisioningFailed = 2,

    /// <summary>
    /// User account is suspended
    /// </summary>
    Suspended = 3,

    /// <summary>
    /// User account is deactivated
    /// </summary>
    Deactivated = 4
}
