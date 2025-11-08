using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Mfa.EnableMfa;

/// <summary>
/// Handler for EnableMfaCommand
/// </summary>
public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, Result<MfaSetupResponseDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly IRecoveryCodeRepository _recoveryCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<EnableMfaCommandHandler> _logger;

    public EnableMfaCommandHandler(
        UserManager<User> userManager,
        IMfaSecretRepository mfaSecretRepository,
        IRecoveryCodeRepository recoveryCodeRepository,
        IUnitOfWork unitOfWork,
        ITotpService totpService,
        IPasswordHasher passwordHasher,
        ILogger<EnableMfaCommandHandler> logger)
    {
        _userManager = userManager;
        _mfaSecretRepository = mfaSecretRepository;
        _recoveryCodeRepository = recoveryCodeRepository;
        _unitOfWork = unitOfWork;
        _totpService = totpService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<MfaSetupResponseDto>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Result.Failure<MfaSetupResponseDto>("User not found");
        }

        if (user.MfaEnabled)
        {
            return Result.Failure<MfaSetupResponseDto>("MFA is already enabled for this user");
        }

        // Generate secret
        var secret = _totpService.GenerateSecret();
        var qrCodeUri = _totpService.GenerateQrCodeUri(user.Email!, secret, "AuthGate");

        // Encrypt secret before storing
        var encryptedSecret = _passwordHasher.HashPassword(secret);

        // Generate recovery codes
        var recoveryCodes = _totpService.GenerateRecoveryCodes(10);
        var recoveryCodeEntities = new List<RecoveryCode>();

        foreach (var code in recoveryCodes)
        {
            var hashedCode = _passwordHasher.HashPassword(code);
            recoveryCodeEntities.Add(new RecoveryCode
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = hashedCode,
                IsUsed = false,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _recoveryCodeRepository.AddRangeAsync(recoveryCodeEntities, cancellationToken);

        // Save MFA secret (not verified yet)
        var mfaSecret = new MfaSecret
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EncryptedSecret = encryptedSecret,
            IsVerified = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _mfaSecretRepository.AddAsync(mfaSecret, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MFA setup initiated for user: {UserId}", user.Id);

        var response = new MfaSetupResponseDto
        {
            SecretKey = secret, // Return plain secret only during setup
            QrCodeDataUri = qrCodeUri,
            ManualEntryKey = FormatSecretKey(secret),
            RecoveryCodes = recoveryCodes // Return plain codes only during setup
        };

        return Result.Success(response);
    }

    private static string FormatSecretKey(string secret)
    {
        // Format as groups of 4 characters for manual entry
        var formatted = string.Join(" ", Enumerable.Range(0, secret.Length / 4)
            .Select(i => secret.Substring(i * 4, Math.Min(4, secret.Length - i * 4))));
        return formatted;
    }
}
