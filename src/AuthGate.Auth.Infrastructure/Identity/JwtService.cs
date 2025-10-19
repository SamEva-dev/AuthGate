using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Infrastructure.Identity;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    public JwtService(IConfiguration config) => _config = config;

    public (string accessToken, string refreshToken, DateTime expiresAtUtc) GenerateTokens(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = int.Parse(_config["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.FullName),
            new("mfa", user.MfaEnabled.ToString())
        };

        var roles = user.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null);
        if (roles != null)
        {
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role!)));
        }

        var token = new JwtSecurityToken(claims: claims, expires: expires, signingCredentials: creds);
        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = Guid.NewGuid().ToString("N");
        return (access, refresh, expires);
    }
}