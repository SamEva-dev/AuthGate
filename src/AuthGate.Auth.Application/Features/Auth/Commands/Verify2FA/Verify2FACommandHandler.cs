using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Verify2FA;

/// <summary>
/// Handler for Verify2FACommand - Validates TOTP and completes login
/// </summary>
public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, Result<LoginResponseDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<Verify2FACommandHandler> _logger;

    public Verify2FACommandHandler(
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        IMfaSecretRepository mfaSecretRepository,
        ITwoFactorService twoFactorService,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        ILogger<Verify2FACommandHandler> logger)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _mfaSecretRepository = mfaSecretRepository;
        _twoFactorService = twoFactorService;
        _jwtService = jwtService;
        _userRoleService = userRoleService;
        _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("2FA verification attempt with token");

            // 1. Validate MFA token and get user ID
            var mfaTokenEntity = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.MfaToken, cancellationToken);
            if (mfaTokenEntity == null || mfaTokenEntity.ExpiresAtUtc < DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired MFA token");
                return Result.Failure<LoginResponseDto>("Invalid or expired MFA token");
            }

            var userId = mfaTokenEntity.UserId;
            _logger.LogDebug("MFA token valid for user {UserId}", userId);

            // 2. Get user
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogError("User not found for MFA verification: {UserId}", userId);
                return Result.Failure<LoginResponseDto>("User not found");
            }

            // 3. Get MFA secret
            var mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(userId, cancellationToken);
            if (mfaSecret == null || !mfaSecret.IsEnabled)
            {
                _logger.LogWarning("2FA not enabled for user {UserId}", userId);
                return Result.Failure<LoginResponseDto>("2FA is not enabled for this account");
            }

            // 4. Validate TOTP code
            if (!_twoFactorService.ValidateCode(mfaSecret.EncryptedSecret, request.Code))
            {
                _logger.LogWarning("Invalid 2FA code for user {UserId}", userId);
                return Result.Failure<LoginResponseDto>("Invalid 2FA code. Please try again.");
            }

            _logger.LogInformation("2FA code validated successfully for user {UserId}", userId);

            // 5. Update MFA last used timestamp
            mfaSecret.UpdateLastUsed();
            _mfaSecretRepository.Update(mfaSecret);

            // 6. Mark temporary MFA token as revoked (one-time use)
            mfaTokenEntity.IsRevoked = true;
            mfaTokenEntity.RevokedAtUtc = DateTime.UtcNow;
            mfaTokenEntity.RevocationReason = "Used for 2FA verification";
            _unitOfWork.RefreshTokens.Update(mfaTokenEntity);

            // 7. Get user roles and permissions
            var roles = await _userRoleService.GetUserRolesAsync(user);
            var permissions = await _userRoleService.GetUserPermissionsAsync(user);

            // 8. Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles, permissions, true, user.TenantId);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();

            // 9. Store refresh token
            var refreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                JwtId = jwtId,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

            // 10. Update last login
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully with 2FA", userId);

            return Result.Success(new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                RequiresMfa = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 2FA verification: {Message}", ex.Message);
            throw;
        }
    }
}
