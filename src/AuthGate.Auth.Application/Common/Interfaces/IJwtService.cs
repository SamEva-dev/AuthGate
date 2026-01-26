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
    /// <param name="organizationId">Organization ID for multi-tenant isolation (required)</param>
    /// <returns>JWT access token</returns>
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool mfaEnabled, Guid organizationId, string? app = null);

    string GeneratePlatformAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool mfaEnabled, bool pwdChangeRequired, Guid? organizationId = null, string? app = null);

    string GenerateTenantAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool mfaEnabled, Guid organizationId, bool pwdChangeRequired, string? app = null);

    /// <summary>
    /// Generates a limited access token for users pending organization provisioning.
    /// This token has restricted access and short expiry.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="email">User email</param>
    /// <param name="roles">User roles</param>
    /// <returns>JWT access token with "pending_provisioning" status claim</returns>
    string GeneratePendingProvisioningToken(Guid userId, string email, IEnumerable<string> roles);

    /// <summary>
    /// Generates a refresh token
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Gets the JWT ID (jti) from a token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The JWT ID</returns>
    string? GetJwtId(string token);

    /// <summary>
    /// Validate an access token signature/issuer/audience and return claims.
    /// Set validateLifetime=false to read claims from an expired token.
    /// </summary>
    ClaimsPrincipal? ValidateAccessToken(string token, bool validateLifetime = true);

    string GenerateMachineToken(string scope);

    string GenerateMachineToken(string scope, string clientId, TimeSpan? lifetime = null, string? audience = null);
}
