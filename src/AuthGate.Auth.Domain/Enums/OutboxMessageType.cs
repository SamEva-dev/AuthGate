namespace AuthGate.Auth.Domain.Enums;

public enum OutboxMessageType
{
    SendConfirmEmail = 0,
    ProvisionOrganization = 1,
    SendWelcomeEmail = 2,
    /// <summary>
    /// Sync user data to LocaGuest
    /// </summary>
    SyncUserToLocaGuest = 3
}
