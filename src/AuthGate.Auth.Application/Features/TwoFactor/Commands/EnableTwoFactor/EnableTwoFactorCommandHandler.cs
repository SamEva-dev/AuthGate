using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AuthGate.Auth.Application.Features.TwoFactor.Commands.EnableTwoFactor;

/// <summary>
/// Handler for enabling 2FA (generates QR code and recovery codes)
/// </summary>
public class EnableTwoFactorCommandHandler : IRequestHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EnableTwoFactorCommandHandler> _logger;

    public EnableTwoFactorCommandHandler(
        IMfaSecretRepository mfaSecretRepository,
        ITwoFactorService twoFactorService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EnableTwoFactorCommandHandler> logger)
    {
        _mfaSecretRepository = mfaSecretRepository;
        _twoFactorService = twoFactorService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<EnableTwoFactorResponse> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        // Get current user ID from JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var userEmail = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ?? "user@locaguest.com";

        // Check if MFA already exists and is enabled
        var existingMfa = await _mfaSecretRepository.GetByUserIdAsync(userId, cancellationToken);
        if (existingMfa?.IsEnabled == true)
        {
            throw new InvalidOperationException("Two-factor authentication is already enabled");
        }

        // Generate new secret and recovery codes
        var secret = _twoFactorService.GenerateSecret();
        var qrCodeUri = _twoFactorService.GenerateQrCodeUri(secret, userEmail);
        var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
        var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();

        // Create or update MFA secret (not enabled yet, waiting for verification)
        if (existingMfa == null)
        {
            var mfaSecret = new MfaSecret
            {
                UserId = userId,
                EncryptedSecret = secret, // TODO: Encrypt in production
                IsVerified = false,
                IsEnabled = false
            };
            mfaSecret.SetRecoveryCodesList(recoveryCodes);

            await _mfaSecretRepository.AddAsync(mfaSecret, cancellationToken);
        }
        else
        {
            existingMfa.EncryptedSecret = secret; // TODO: Encrypt in production
            existingMfa.IsVerified = false;
            existingMfa.IsEnabled = false;
            existingMfa.SetRecoveryCodesList(recoveryCodes);

            _mfaSecretRepository.Update(existingMfa);
        }

        _logger.LogInformation("2FA setup initiated for user {UserId}", userId);

        return new EnableTwoFactorResponse
        {
            Secret = secret,
            QrCodeUri = qrCodeUri,
            QrCodeImage = qrCodeImage,
            RecoveryCodes = recoveryCodes
        };
    }
}
