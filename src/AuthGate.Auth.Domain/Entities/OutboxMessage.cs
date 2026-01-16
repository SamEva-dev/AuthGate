using AuthGate.Auth.Domain.Enums;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a message in the transactional outbox for reliable async processing.
/// Implements the Outbox Pattern for distributed transactions.
/// </summary>
public class OutboxMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of message to process
    /// </summary>
    public OutboxMessageType Type { get; set; }

    /// <summary>
    /// JSON payload containing the message data
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Related entity ID (e.g., UserId for ProvisionOrganization)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// When the message was successfully processed (null if not yet processed)
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Maximum number of retries before marking as failed
    /// </summary>
    public int MaxRetries { get; set; } = 10;

    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// When to next attempt processing (for exponential backoff)
    /// </summary>
    public DateTime? NextRetryAtUtc { get; set; }

    /// <summary>
    /// Whether the message has permanently failed
    /// </summary>
    public bool IsFailed { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Creates a new outbox message
    /// </summary>
    public static OutboxMessage Create(
        OutboxMessageType type,
        string payload,
        Guid? relatedEntityId = null,
        string? correlationId = null)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            RelatedEntityId = relatedEntityId,
            CreatedAtUtc = DateTime.UtcNow,
            NextRetryAtUtc = DateTime.UtcNow,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Marks the message as successfully processed
    /// </summary>
    public void MarkAsProcessed()
    {
        ProcessedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed processing attempt with exponential backoff
    /// </summary>
    public void RecordFailure(string error)
    {
        RetryCount++;
        LastError = error;

        if (RetryCount >= MaxRetries)
        {
            IsFailed = true;
            NextRetryAtUtc = null;
        }
        else
        {
            // Exponential backoff: 2^retry seconds, max 1 hour
            var delaySeconds = Math.Min(Math.Pow(2, RetryCount), 3600);
            NextRetryAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
        }
    }

    /// <summary>
    /// Checks if the message is ready for processing
    /// </summary>
    public bool IsReadyForProcessing()
    {
        return ProcessedAtUtc == null 
               && !IsFailed 
               && (NextRetryAtUtc == null || NextRetryAtUtc <= DateTime.UtcNow);
    }
}
