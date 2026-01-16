namespace AuthGate.Auth.Domain.Enums;

/// <summary>
/// Types of outbox messages for async processing
/// </summary>
public enum OutboxMessageType
{
    /// <summary>
    /// Provision a new organization in LocaGuest
    /// </summary>
    ProvisionOrganization = 1,

    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    SendWelcomeEmail = 2,

    /// <summary>
    /// Sync user data to LocaGuest
    /// </summary>
    SyncUserToLocaGuest = 3
}
