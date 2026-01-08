using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Service for generating and managing authentication tokens
/// </summary>
public class TokenService : ITokenService
{
    private readonly UserManager<User> _userManager;
    private readonly AuthDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly IConfiguration _configuration;

    public TokenService(
        UserManager<User> userManager,
        AuthDbContext context,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
        _userRoleService = userRoleService;
        _configuration = configuration;
    }

    public async Task<(string AccessToken, string RefreshToken)> GenerateTokensAsync(User user)
    {
        // Get user roles and permissions
        var roles = await _userRoleService.GetUserRolesAsync(user);
        var permissions = await _userRoleService.GetUserPermissionsAsync(user);

        if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot issue access token without OrganizationId.");
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(
            user.Id,
            user.Email!,
            roles,
            permissions,
            user.MfaEnabled,
            user.OrganizationId.Value
        );

        var refreshToken = _jwtService.GenerateRefreshToken();
        var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();

        var refreshTokenHash = HashRefreshToken(refreshToken);

        // Store refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenHash,
            JwtId = jwtId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return (accessToken, refreshToken);
    }

    public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken)
    {
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken || rt.Token == refreshTokenHash);

        if (storedToken == null || storedToken.IsRevoked || storedToken.IsUsed || storedToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return null;
        }

        // Mark old token as used
        storedToken.IsUsed = true;
        await _context.SaveChangesAsync();

        // Get user
        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null)
        {
            return null;
        }

        // Generate new tokens
        return await GenerateTokensAsync(user);
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var refreshTokenHash = HashRefreshToken(refreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken || rt.Token == refreshTokenHash);

        if (storedToken == null)
        {
            return false;
        }

        storedToken.IsRevoked = true;
        await _context.SaveChangesAsync();

        return true;
    }

    private string HashRefreshToken(string refreshToken)
    {
        var pepper = _configuration["Jwt:RefreshTokenPepper"];
        if (string.IsNullOrWhiteSpace(pepper))
        {
            pepper = _configuration["Security:RefreshTokenPepper"];
        }

        if (string.IsNullOrWhiteSpace(pepper))
        {
            return refreshToken;
        }

        var input = Encoding.UTF8.GetBytes(refreshToken + pepper);
        var hash = SHA256.HashData(input);
        return Convert.ToHexString(hash);
    }
}
