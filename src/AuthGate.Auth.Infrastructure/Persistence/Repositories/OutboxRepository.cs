using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for OutboxMessage entities
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AuthDbContext _context;

    public OutboxRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null 
                        && !m.IsFailed 
                        && (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= DateTime.UtcNow))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.OutboxMessages.Update(message);
        return Task.CompletedTask;
    }

    public async Task<OutboxMessage?> GetByRelatedEntityIdAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.RelatedEntityId == entityId && m.ProcessedAtUtc == null)
            .OrderByDescending(m => m.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetFailedMessagesAsync(int pageSize, int page, CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(m => m.IsFailed)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
