namespace AuthGate.Auth.Infrastructure.Options;

public sealed class MachineTokenOptions
{
    public string ClientId { get; set; } = "authgate";
    public string Audience { get; set; } = "LocaGuest";
    public int TokenLifetimeMinutes { get; set; } = 2;
}
