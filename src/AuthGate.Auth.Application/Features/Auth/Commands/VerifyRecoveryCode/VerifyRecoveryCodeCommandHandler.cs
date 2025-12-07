using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.VerifyRecoveryCode;

/// <summary>
/// Handler for VerifyRecoveryCodeCommand - Validates recovery code and completes login
/// </summary>
public class VerifyRecoveryCodeCommandHandler : IRequestHandler<VerifyRecoveryCodeCommand, Result<LoginResponseDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<VerifyRecoveryCodeCommandHandler> _logger;

    public VerifyRecoveryCodeCommandHandler(
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        IMfaSecretRepository mfaSecretRepository,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        ILogger<VerifyRecoveryCodeCommandHandler> logger)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _mfaSecretRepository = mfaSecretRepository;
        _jwtService = jwtService;
        _userRoleService = userRoleService;
        _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> Handle(VerifyRecoveryCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Recovery code verification attempt");

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
                _logger.LogError("User not found for recovery code verification: {UserId}", userId);
                return Result.Failure<LoginResponseDto>("User not found");
            }

            // 3. Get MFA secret
            var mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(userId, cancellationToken);
            if (mfaSecret == null || !mfaSecret.IsEnabled)
            {
                _logger.LogWarning("2FA not enabled for user {UserId}", userId);
                return Result.Failure<LoginResponseDto>("2FA is not enabled for this account");
            }

            // 4. Check if recovery codes are available
            if (mfaSecret.RecoveryCodesRemaining <= 0)
            {
                _logger.LogWarning("No recovery codes remaining for user {UserId}", userId);
                return Result.Failure<LoginResponseDto>("No recovery codes available. Please contact your administrator.");
            }

            // 5. Validate and consume recovery code
            var codeUsed = mfaSecret.UseRecoveryCode(request.RecoveryCode);
            if (!codeUsed)
            {
                _logger.LogWarning("Invalid recovery code for user {UserId}", userId);
                return Result.Failure<LoginResponseDto>("Invalid recovery code. Please check and try again.");
            }

            _logger.LogInformation("Recovery code validated successfully for user {UserId}. {RemainingCodes} codes remaining", 
                userId, mfaSecret.RecoveryCodesRemaining);

            // 6. Update MFA secret (recovery codes updated by UseRecoveryCode)
            mfaSecret.UpdateLastUsed();
            _mfaSecretRepository.Update(mfaSecret);

            // 7. Mark temporary MFA token as revoked (one-time use)
            mfaTokenEntity.IsRevoked = true;
            mfaTokenEntity.RevokedAtUtc = DateTime.UtcNow;
            mfaTokenEntity.RevocationReason = "Used for recovery code verification";
            _unitOfWork.RefreshTokens.Update(mfaTokenEntity);

            // 8. Get user roles and permissions
            var roles = await _userRoleService.GetUserRolesAsync(user);
            var permissions = await _userRoleService.GetUserPermissionsAsync(user);

            // 9. Generate JWT tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles, permissions, true, user.TenantId);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();

            // 10. Store refresh token
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

            // 11. Update last login
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 12. Warn if low on recovery codes
            var warningMessage = mfaSecret.RecoveryCodesRemaining <= 2
                ? $"Warning: Only {mfaSecret.RecoveryCodesRemaining} recovery code(s) remaining. Please generate new codes."
                : null;

            _logger.LogInformation("User {UserId} logged in successfully with recovery code", userId);

            return Result.Success(new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                RequiresMfa = false,
                // TODO: Add warning message to DTO if needed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during recovery code verification: {Message}", ex.Message);
            throw;
        }
    }
}
