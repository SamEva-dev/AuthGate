using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // With Identity, we load user with roles via join on Identity tables
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
            return null;

        // Load roles and permissions separately (Identity approach)
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var rolesWithPermissions = await _context.Roles
            .Where(r => userRoleIds.Contains(r.Id))
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        // Store roles in a way accessible to handlers (we'll use a helper property or method)
        return user;
    }

    public async Task<IEnumerable<User>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.CountAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public void Delete(User user)
    {
        _context.Users.Remove(user);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var query = _context.Users.Where(u => u.NormalizedEmail == normalizedEmail);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
