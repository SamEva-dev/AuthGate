namespace AuthGate.Auth.Infrastructure.Options;

/// <summary>
/// Configuration options for the OutboxProcessor background service.
/// </summary>
public class OutboxProcessorOptions
{
    public const string SectionName = "OutboxProcessor";

    /// <summary>
    /// Polling interval in seconds between outbox processing cycles.
    /// Default: 5 seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum number of messages to process per batch.
    /// Default: 10 messages.
    /// </summary>
    public int BatchSize { get; set; } = 10;
}
