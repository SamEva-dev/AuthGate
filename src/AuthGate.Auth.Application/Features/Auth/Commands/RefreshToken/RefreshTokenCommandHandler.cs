using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace AuthGate.Auth.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<TokenResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        ILogger<RefreshTokenCommandHandler> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _userRoleService = userRoleService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Result<TokenResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Get refresh token from database
        var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token not found");
            return Result.Failure<TokenResponseDto>("Invalid refresh token");
        }

        // Check if token is expired
        if (refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired refresh token used: {TokenId}", refreshToken.Id);
            return Result.Failure<TokenResponseDto>("Refresh token has expired");
        }

        // Check if token is revoked
        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning("Revoked refresh token used: {TokenId}, revoking all user tokens", refreshToken.Id);
            
            // Potential token reuse attack - revoke all user tokens
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(
                refreshToken.UserId,
                "Potential token reuse detected",
                cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<TokenResponseDto>("Invalid refresh token");
        }

        // Check if token is already used
        if (refreshToken.IsUsed)
        {
            _logger.LogWarning("Used refresh token reused: {TokenId}, revoking all user tokens", refreshToken.Id);
            
            // Token reuse detected - revoke all user tokens
            await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(
                refreshToken.UserId,
                "Token reuse detected",
                cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Failure<TokenResponseDto>("Invalid refresh token");
        }

        // Get user with roles and permissions
        var user = await _unitOfWork.Users.GetByIdWithRolesAndPermissionsAsync(refreshToken.UserId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("Refresh token used for inactive/non-existent user: {UserId}", refreshToken.UserId);
            return Result.Failure<TokenResponseDto>("User account is not active");
        }

        // Generate new tokens using Identity
        var roles = await _userRoleService.GetUserRolesAsync(user);
        var permissions = await _userRoleService.GetUserPermissionsAsync(user);

        if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
        {
            return Result.Failure<TokenResponseDto>("Cannot issue access token without OrganizationId.");
        }

        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles, permissions, user.MfaEnabled, user.OrganizationId.Value);
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var jwtId = _jwtService.GetJwtId(newAccessToken) ?? Guid.NewGuid().ToString();

        var newRefreshTokenHash = HashRefreshToken(newRefreshToken);

        // Mark old token as used
        refreshToken.IsUsed = true;
        _unitOfWork.RefreshTokens.Update(refreshToken);

        // Create new refresh token
        var newRefreshTokenEntity = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenHash,
            JwtId = jwtId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Link old token to new token for rotation tracking
        refreshToken.ReplacedByTokenId = newRefreshTokenEntity.Id;

        await _unitOfWork.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tokens refreshed for user {UserId}", user.Id);

        return Result.Success(new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 900 // 15 minutes
        });
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
