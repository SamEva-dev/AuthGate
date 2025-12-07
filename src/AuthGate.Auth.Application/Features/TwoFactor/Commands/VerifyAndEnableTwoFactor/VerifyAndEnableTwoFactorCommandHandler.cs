using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AuthGate.Auth.Application.Features.TwoFactor.Commands.VerifyAndEnableTwoFactor;

/// <summary>
/// Handler for verifying TOTP code and enabling 2FA
/// </summary>
public class VerifyAndEnableTwoFactorCommandHandler : IRequestHandler<VerifyAndEnableTwoFactorCommand, bool>
{
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<VerifyAndEnableTwoFactorCommandHandler> _logger;

    public VerifyAndEnableTwoFactorCommandHandler(
        IMfaSecretRepository mfaSecretRepository,
        ITwoFactorService twoFactorService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<VerifyAndEnableTwoFactorCommandHandler> logger)
    {
        _mfaSecretRepository = mfaSecretRepository;
        _twoFactorService = twoFactorService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> Handle(VerifyAndEnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        // Get current user ID from JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Get MFA secret
        var mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(userId, cancellationToken);
        if (mfaSecret == null)
        {
            throw new InvalidOperationException("Two-factor authentication not initialized. Please enable it first.");
        }

        // Validate TOTP code
        if (!_twoFactorService.ValidateCode(mfaSecret.EncryptedSecret, request.Code))
        {
            _logger.LogWarning("Invalid 2FA code attempt for user {UserId}", userId);
            throw new InvalidOperationException("Invalid verification code. Please try again.");
        }

        // Enable 2FA
        mfaSecret.Enable();
        mfaSecret.UpdateLastUsed();
        
        _mfaSecretRepository.Update(mfaSecret);

        _logger.LogInformation("2FA successfully enabled for user {UserId}", userId);

        return true;
    }
}
