using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for MfaSecret entity
/// </summary>
public class MfaSecretRepository : IMfaSecretRepository
{
    private readonly AuthDbContext _context;

    public MfaSecretRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<MfaSecret?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MfaSecret>()
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);
    }

    public async Task<MfaSecret?> GetVerifiedByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<MfaSecret>()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.IsVerified, cancellationToken);
    }

    public async Task AddAsync(MfaSecret mfaSecret, CancellationToken cancellationToken = default)
    {
        await _context.Set<MfaSecret>().AddAsync(mfaSecret, cancellationToken);
    }

    public void Update(MfaSecret mfaSecret)
    {
        _context.Set<MfaSecret>().Update(mfaSecret);
    }

    public async Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var secrets = await _context.Set<MfaSecret>()
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.Set<MfaSecret>().RemoveRange(secrets);
    }
}
