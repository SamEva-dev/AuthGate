namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// DTO for login response
/// </summary>
public record LoginResponseDto
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// Refresh token
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Token expiration time in seconds
    /// </summary>
    public int? ExpiresIn { get; init; }

    /// <summary>
    /// Whether MFA is required
    /// </summary>
    public bool RequiresMfa { get; init; }

    /// <summary>
    /// Temporary token for MFA verification (if MFA is enabled)
    /// </summary>
    public string? MfaToken { get; init; }

    public bool PasswordChangeRequired { get; init; }

    public DateTime? PasswordChangeBeforeUtc { get; init; }
}
