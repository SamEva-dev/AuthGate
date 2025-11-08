using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AuthDbContext _context;

    public PermissionRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Permissions.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpperInvariant();
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.NormalizedCode == normalizedCode, cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Permissions.AsQueryable();

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Permission>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // With Identity, we need to join via IdentityUserRole table
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        return await _context.Permissions
            .Where(p => p.RolePermissions.Any(rp => userRoleIds.Contains(rp.RoleId)))
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IEnumerable<Permission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetPermissionsForUserAsync(userId, cancellationToken);
    }

    public async Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        await _context.Permissions.AddAsync(permission, cancellationToken);
    }

    public void Update(Permission permission)
    {
        _context.Permissions.Update(permission);
    }

    public void Delete(Permission permission)
    {
        _context.Permissions.Remove(permission);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludePermissionId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.ToUpperInvariant();
        var query = _context.Permissions.Where(p => p.NormalizedCode == normalizedCode);

        if (excludePermissionId.HasValue)
            query = query.Where(p => p.Id != excludePermissionId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
