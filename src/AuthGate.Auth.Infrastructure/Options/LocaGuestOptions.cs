namespace AuthGate.Auth.Infrastructure.Options;

public sealed class LocaGuestOptions
{
    public string ApiBaseUrl { get; set; } = string.Empty;
    public ResilienceOptions Resilience { get; set; } = new();
}

public sealed class ResilienceOptions
{
    public int AttemptTimeoutSeconds { get; set; } = 5;
    public int TotalTimeoutSeconds { get; set; } = 20;
    public int MaxRetries { get; set; } = 2;

    public int BreakDurationSeconds { get; set; } = 30;
    public int MinimumThroughput { get; set; } = 10;
    public double FailureRatio { get; set; } = 0.5;
}
