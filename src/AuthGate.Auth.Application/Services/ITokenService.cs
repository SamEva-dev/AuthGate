using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Services;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate access and refresh tokens for a user
    /// </summary>
    Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(User user);

    /// <summary>
    /// Generate limited tokens for a user pending organization provisioning
    /// </summary>
    Task<(string AccessToken, string RefreshToken)> GeneratePendingProvisioningTokensAsync(User user);

    /// <summary>
    /// Validate and refresh an access token
    /// </summary>
    Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    Task<bool> RevokeTokenAsync(string refreshToken);
}
