using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for Permission entity operations
/// </summary>
public interface IPermissionRepository
{
    /// <summary>
    /// Gets a permission by its unique identifier
    /// </summary>
    /// <param name="id">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission if found, null otherwise</returns>
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a permission by its code
    /// </summary>
    /// <param name="code">The permission code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The permission if found, null otherwise</returns>
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions with optional pagination
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of permissions</returns>
    Task<IEnumerable<Permission>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of permissions</returns>
    Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a specific user (through their roles)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of permissions</returns>
    Task<IEnumerable<Permission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new permission
    /// </summary>
    /// <param name="permission">The permission to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing permission
    /// </summary>
    /// <param name="permission">The permission to update</param>
    void Update(Permission permission);

    /// <summary>
    /// Deletes a permission
    /// </summary>
    /// <param name="permission">The permission to delete</param>
    void Delete(Permission permission);

    /// <summary>
    /// Checks if a permission code already exists
    /// </summary>
    /// <param name="code">The permission code to check</param>
    /// <param name="excludePermissionId">Optional permission ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code exists, false otherwise</returns>
    Task<bool> CodeExistsAsync(string code, Guid? excludePermissionId = null, CancellationToken cancellationToken = default);
}
