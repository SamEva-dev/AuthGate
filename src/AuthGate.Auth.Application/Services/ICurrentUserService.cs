namespace AuthGate.Auth.Application.Services;

/// <summary>
/// Service to access current authenticated user information
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user ID
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Checks if the user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's roles
    /// </summary>
    IEnumerable<string> Roles { get; }
}
