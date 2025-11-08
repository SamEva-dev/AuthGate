namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// Response DTO for user registration
/// </summary>
public class RegisterResponseDto
{
    /// <summary>
    /// User ID of the newly created user
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Message about email confirmation if required
    /// </summary>
    public string? Message { get; set; }
}
