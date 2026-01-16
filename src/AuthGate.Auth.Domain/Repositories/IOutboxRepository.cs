using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for OutboxMessage entities
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a new outbox message
    /// </summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages ready for processing
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an outbox message
    /// </summary>
    Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a message by its related entity ID
    /// </summary>
    Task<OutboxMessage?> GetByRelatedEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed messages for manual review
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetFailedMessagesAsync(int pageSize, int page, CancellationToken cancellationToken = default);
}
