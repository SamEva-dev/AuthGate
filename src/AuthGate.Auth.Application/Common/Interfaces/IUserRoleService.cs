using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Service for managing user roles and permissions with Identity
/// </summary>
public interface IUserRoleService
{
    /// <summary>
    /// Gets all role names for a user
    /// </summary>
    Task<List<string>> GetUserRolesAsync(User user);

    /// <summary>
    /// Gets all permission codes for a user (via their roles)
    /// </summary>
    Task<List<string>> GetUserPermissionsAsync(User user);

    /// <summary>
    /// Checks if user has a specific permission
    /// </summary>
    Task<bool> UserHasPermissionAsync(User user, string permissionCode);
}
