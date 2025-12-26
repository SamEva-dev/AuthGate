namespace AuthGate.Auth.Infrastructure.Jobs;

public class AuditRetentionOptions
{
    public int Days { get; set; } = 7;
    public int RunIntervalHours { get; set; } = 24;
}
