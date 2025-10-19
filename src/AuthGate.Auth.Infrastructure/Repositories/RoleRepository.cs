using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace AuthGate.Auth.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AuthGateDbContext _db;

    public RoleRepository(AuthGateDbContext db) => _db = db;

    public async Task<IEnumerable<Role>> GetAllAsync() => await _db.Roles.ToListAsync();
    public Task<Role?> GetByIdAsync(Guid id) => _db.Roles.FirstOrDefaultAsync(r => r.Id == id);
    public Task<Role?> GetByNameAsync(string name) => _db.Roles.FirstOrDefaultAsync(r => r.Name == name);

    public async Task AddAsync(Role role)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role is null) return;
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId)
    {
        var exists = await _db.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        if (!exists)
        {
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var rel = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        if (rel is not null)
        {
            _db.UserRoles.Remove(rel);
            await _db.SaveChangesAsync();
        }
    }
}