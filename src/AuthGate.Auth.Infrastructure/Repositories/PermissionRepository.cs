
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthGate.Auth.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly AuthGateDbContext _db;
    public PermissionRepository(AuthGateDbContext db) => _db = db;

    public async Task<IEnumerable<Permission>> GetAllAsync() => await _db.Permissions.ToListAsync();
    public Task<Permission?> GetByCodeAsync(string code) => _db.Permissions.FirstOrDefaultAsync(p => p.Code == code);

    public async Task AddAsync(Permission permission)
    {
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync();
    }

    public async Task AssignToRoleAsync(Guid roleId, Guid permissionId)
    {
        if (!await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rel = await _db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        if (rel is not null)
        {
            _db.RolePermissions.Remove(rel);
            await _db.SaveChangesAsync();
        }
    }
}