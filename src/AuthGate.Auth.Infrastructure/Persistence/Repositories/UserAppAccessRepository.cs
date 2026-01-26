using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class UserAppAccessRepository : IUserAppAccessRepository
{
    private readonly AuthDbContext _context;

    public UserAppAccessRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasAccessAsync(Guid userId, string appId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserAppAccess>()
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.AppId == appId, cancellationToken);
    }

    public async Task AddAsync(UserAppAccess access, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserAppAccess>().AddAsync(access, cancellationToken);
    }
}
