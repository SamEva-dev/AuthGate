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

    /// <summary>
    /// Current access token (optional) used to preserve app + org context during refresh
    /// </summary>
    public string? AccessToken { get; init; }
}
