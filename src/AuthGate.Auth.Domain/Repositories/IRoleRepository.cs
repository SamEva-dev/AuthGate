using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for Role entity operations
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Gets a role by its unique identifier
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by its name
    /// </summary>
    /// <param name="name">The role name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role if found, null otherwise</returns>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles with optional pagination
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of roles</returns>
    Task<IEnumerable<Role>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role with its permissions included
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The role with permissions if found, null otherwise</returns>
    Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new role
    /// </summary>
    /// <param name="role">The role to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role
    /// </summary>
    /// <param name="role">The role to update</param>
    void Update(Role role);

    /// <summary>
    /// Deletes a role
    /// </summary>
    /// <param name="role">The role to delete</param>
    void Delete(Role role);

    /// <summary>
    /// Checks if a role name already exists
    /// </summary>
    /// <param name="name">The role name to check</param>
    /// <param name="excludeRoleId">Optional role ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if role name exists, false otherwise</returns>
    Task<bool> NameExistsAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);
}
