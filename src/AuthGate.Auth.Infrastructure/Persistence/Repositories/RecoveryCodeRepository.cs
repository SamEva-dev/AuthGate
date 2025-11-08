using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for RecoveryCode entity
/// </summary>
public class RecoveryCodeRepository : IRecoveryCodeRepository
{
    private readonly AuthDbContext _context;

    public RecoveryCodeRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RecoveryCode>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RecoveryCode>()
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RecoveryCode>> GetUnusedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RecoveryCode>()
            .Where(r => r.UserId == userId && !r.IsUsed)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RecoveryCode recoveryCode, CancellationToken cancellationToken = default)
    {
        await _context.Set<RecoveryCode>().AddAsync(recoveryCode, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<RecoveryCode> recoveryCodes, CancellationToken cancellationToken = default)
    {
        await _context.Set<RecoveryCode>().AddRangeAsync(recoveryCodes, cancellationToken);
    }

    public void Update(RecoveryCode recoveryCode)
    {
        _context.Set<RecoveryCode>().Update(recoveryCode);
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var codes = await _context.Set<RecoveryCode>()
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.Set<RecoveryCode>().RemoveRange(codes);
    }
}
