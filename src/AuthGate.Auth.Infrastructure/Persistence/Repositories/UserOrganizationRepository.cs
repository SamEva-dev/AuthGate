using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class UserOrganizationRepository : IUserOrganizationRepository
{
    private readonly AuthDbContext _context;

    public UserOrganizationRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserOrganization>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.UserOrganizations
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.OrganizationId == organizationId, cancellationToken);
    }

    public async Task AddAsync(UserOrganization userOrganization, CancellationToken cancellationToken = default)
    {
        await _context.UserOrganizations.AddAsync(userOrganization, cancellationToken);
    }

    public void Remove(UserOrganization userOrganization)
    {
        _context.UserOrganizations.Remove(userOrganization);
    }
}
