using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToUpperInvariant();
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
    }

    public async Task<IEnumerable<Role>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Roles.AsQueryable();

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
    }

    public void Update(Role role)
    {
        _context.Roles.Update(role);
    }

    public void Delete(Role role)
    {
        _context.Roles.Remove(role);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToUpperInvariant();
        var query = _context.Roles.Where(r => r.NormalizedName == normalizedName);

        if (excludeRoleId.HasValue)
            query = query.Where(r => r.Id != excludeRoleId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
