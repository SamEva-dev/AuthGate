using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique identifier
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    /// <param name="email">The email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their roles included
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user with roles if found, null otherwise</returns>
    Task<User?> GetByIdWithRolesAndPermissionsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with optional pagination
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of users</returns>
    Task<IEnumerable<User>> GetAllAsync(int? skip = null, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of users</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new user
    /// </summary>
    /// <param name="user">The user to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">The user to update</param>
    void Update(User user);

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="user">The user to delete</param>
    void Delete(User user);

    /// <summary>
    /// Checks if an email already exists
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="excludeUserId">Optional user ID to exclude from check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}
