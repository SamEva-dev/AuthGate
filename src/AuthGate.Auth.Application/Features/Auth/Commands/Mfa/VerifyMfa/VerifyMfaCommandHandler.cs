using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Mfa.VerifyMfa;

/// <summary>
/// Handler for VerifyMfaCommand
/// </summary>
public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITotpService _totpService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<VerifyMfaCommandHandler> _logger;

    public VerifyMfaCommandHandler(
        UserManager<User> userManager,
        IMfaSecretRepository mfaSecretRepository,
        IUnitOfWork unitOfWork,
        ITotpService totpService,
        IPasswordHasher passwordHasher,
        ILogger<VerifyMfaCommandHandler> logger)
    {
        _userManager = userManager;
        _mfaSecretRepository = mfaSecretRepository;
        _unitOfWork = unitOfWork;
        _totpService = totpService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Result.Failure<bool>("User not found");
        }

        // Get MFA secret
        var mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(user.Id, cancellationToken);

        if (mfaSecret == null)
        {
            return Result.Failure<bool>("MFA setup not initiated");
        }

        if (mfaSecret.IsVerified)
        {
            return Result.Failure<bool>("MFA is already verified");
        }

        // Note: We need to store the plain secret temporarily during setup
        // For production, consider storing it encrypted and decrypt here
        // For now, we verify against the code directly
        var isValid = _totpService.VerifyCode(request.Secret, request.Code);
        
        if (!isValid)
        {
            _logger.LogWarning("Invalid MFA code provided for user: {UserId}", user.Id);
            return Result.Failure<bool>("Invalid verification code");
        }

        // Mark as verified and enable MFA
        mfaSecret.IsVerified = true;
        mfaSecret.VerifiedAtUtc = DateTime.UtcNow;
        _mfaSecretRepository.Update(mfaSecret);

        user.MfaEnabled = true;
        await _userManager.UpdateAsync(user);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MFA enabled and verified for user: {UserId}", user.Id);

        return Result.Success(true);
    }
}
