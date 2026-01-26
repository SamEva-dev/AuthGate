using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthGate.Auth.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly RsaKeyService _rsaKeyService;

    public JwtService(IConfiguration configuration, RsaKeyService rsaKeyService)
    {
        _configuration = configuration;
        _tokenHandler = new JwtSecurityTokenHandler();
        _rsaKeyService = rsaKeyService;
    }

    private static string NormalizeApp(string? app)
    {
        return string.IsNullOrWhiteSpace(app) ? "locaguest" : app.Trim();
    }

    public string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool mfaEnabled, Guid organizationId, string? app = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot issue access token without OrganizationId.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("mfa_enabled", mfaEnabled.ToString().ToLower()),
            new(ClaimNames.OrgId, organizationId.ToString("D")),
            new(ClaimNames.OrganizationId, organizationId.ToString("D"))
        };

        claims.Add(new Claim(ClaimNames.App, NormalizeApp(app)));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimNames.Roles, role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(ClaimNames.Permissions, permission));
            claims.Add(new Claim("permission", permission));
        }

        var signingKey = _rsaKeyService.GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GeneratePlatformAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool mfaEnabled, bool pwdChangeRequired, Guid? organizationId = null, string? app = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("mfa_enabled", mfaEnabled.ToString().ToLower()),
            new(ClaimNames.PlatformAdmin, "true"),
            new(ClaimNames.PasswordChangeRequired, pwdChangeRequired.ToString().ToLower())
        };

        claims.Add(new Claim(ClaimNames.App, NormalizeApp(app)));

        if (organizationId.HasValue && organizationId.Value != Guid.Empty)
        {
            claims.Add(new Claim(ClaimNames.OrgId, organizationId.Value.ToString("D")));
            claims.Add(new Claim(ClaimNames.OrganizationId, organizationId.Value.ToString("D")));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimNames.Roles, role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(ClaimNames.Permissions, permission));
            claims.Add(new Claim("permission", permission));
        }

        var signingKey = _rsaKeyService.GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateTenantAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, bool mfaEnabled, Guid organizationId, bool pwdChangeRequired, string? app = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot issue access token without OrganizationId.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("mfa_enabled", mfaEnabled.ToString().ToLower()),
            new(ClaimNames.OrgId, organizationId.ToString("D")),
            new(ClaimNames.OrganizationId, organizationId.ToString("D")),
            new(ClaimNames.PasswordChangeRequired, pwdChangeRequired.ToString().ToLower())
        };

        claims.Add(new Claim(ClaimNames.App, NormalizeApp(app)));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimNames.Roles, role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim(ClaimNames.Permissions, permission));
            claims.Add(new Claim("permission", permission));
        }

        var signingKey = _rsaKeyService.GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GeneratePendingProvisioningToken(Guid userId, string email, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("status", "pending_provisioning"),
            new("mfa_enabled", "false"),
            new(ClaimNames.App, "locaguest")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimNames.Roles, role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = _rsaKeyService.GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        // Short-lived token (5 minutes) - user should refresh after provisioning completes
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public string GenerateMachineToken(string scope)
    {
        return GenerateMachineToken(scope, clientId: "authgate", lifetime: TimeSpan.FromMinutes(15), audience: null);
    }

    public string GenerateMachineToken(string scope, string clientId, TimeSpan? lifetime = null, string? audience = null)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("clientId is required.", nameof(clientId));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, clientId),
            new("azp", clientId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("scope", scope)
        };

        var signingKey = _rsaKeyService.GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: audience ?? _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(15)),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public string? GetJwtId(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }
}
