using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditDbContext _context;

    public AuditLogRepository(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAtUtc);

        if (skip.HasValue)
            query = (IOrderedQueryable<AuditLog>)query.Skip(skip.Value);

        if (take.HasValue)
            query = (IOrderedQueryable<AuditLog>)query.Take(take.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(AuditAction action, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .Where(al => al.Action == action)
            .OrderByDescending(al => al.CreatedAtUtc);

        if (skip.HasValue)
            query = (IOrderedQueryable<AuditLog>)query.Skip(skip.Value);

        if (take.HasValue)
            query = (IOrderedQueryable<AuditLog>)query.Take(take.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDateUtc, DateTime endDateUtc, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs
            .Where(al => al.CreatedAtUtc >= startDateUtc && al.CreatedAtUtc <= endDateUtc)
            .OrderByDescending(al => al.CreatedAtUtc);

        if (skip.HasValue)
            query = (IOrderedQueryable<AuditLog>)query.Skip(skip.Value);

        if (take.HasValue)
            query = (IOrderedQueryable<AuditLog>)query.Take(take.Value);

        return await query.ToListAsync(cancellationToken);
    }
}
