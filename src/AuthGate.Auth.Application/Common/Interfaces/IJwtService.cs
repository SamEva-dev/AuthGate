using System.Security.Claims;

namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Service interface for JWT token generation and validation
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates an access token for the specified user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="roles">User roles</param>
    /// <param name="permissions">User permissions</param>
    /// <param name="mfaEnabled">Whether MFA is enabled</param>
    /// <param name="tenantId">Tenant ID for multi-tenant isolation (optional)</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool mfaEnabled, Guid? tenantId = null);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token and returns its claims
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>Claims principal if valid, null otherwise</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Gets the JWT ID (jti) from a token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The JWT ID</returns>
    string? GetJwtId(string token);
}
