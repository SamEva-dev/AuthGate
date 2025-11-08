namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// DTO for refresh token request
/// </summary>
public record RefreshTokenDto
{
    /// <summary>
    /// Refresh token
    /// </summary>
    public required string RefreshToken { get; init; }
}
