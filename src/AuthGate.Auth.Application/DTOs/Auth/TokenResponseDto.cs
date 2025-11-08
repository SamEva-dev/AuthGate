namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// DTO for token response
/// </summary>
public record TokenResponseDto
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Refresh token
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public required int ExpiresIn { get; init; }
}
