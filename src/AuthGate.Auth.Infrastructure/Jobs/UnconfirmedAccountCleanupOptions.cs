namespace AuthGate.Auth.Infrastructure.Jobs;

public class UnconfirmedAccountCleanupOptions
{
    public int RunIntervalMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 100;
}
