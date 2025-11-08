using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Service for managing user roles and permissions with Identity (hybrid approach)
/// </summary>
public class UserRoleService : IUserRoleService
{
    private readonly UserManager<User> _userManager;
    private readonly AuthDbContext _context;

    public UserRoleService(UserManager<User> userManager, AuthDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<List<string>> GetUserRolesAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<List<string>> GetUserPermissionsAsync(User user)
    {
        // Get user's role IDs via Identity
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        // Get permissions from roles
        var permissions = await _context.RolePermissions
            .Where(rp => userRoleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        return permissions;
    }

    public async Task<bool> UserHasPermissionAsync(User user, string permissionCode)
    {
        var permissions = await GetUserPermissionsAsync(user);
        return permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }
}
