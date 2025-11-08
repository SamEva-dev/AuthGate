namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// DTO for user login request
/// </summary>
public record LoginDto
{
    /// <summary>
    /// User email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User password
    /// </summary>
    public required string Password { get; init; }
}
