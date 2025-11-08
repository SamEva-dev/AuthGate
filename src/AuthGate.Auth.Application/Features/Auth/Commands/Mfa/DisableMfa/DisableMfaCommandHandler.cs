using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Mfa.DisableMfa;

/// <summary>
/// Handler for DisableMfaCommand
/// </summary>
public class DisableMfaCommandHandler : IRequestHandler<DisableMfaCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly IRecoveryCodeRepository _recoveryCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DisableMfaCommandHandler> _logger;

    public DisableMfaCommandHandler(
        UserManager<User> userManager,
        IMfaSecretRepository mfaSecretRepository,
        IRecoveryCodeRepository recoveryCodeRepository,
        IUnitOfWork unitOfWork,
        ILogger<DisableMfaCommandHandler> logger)
    {
        _userManager = userManager;
        _mfaSecretRepository = mfaSecretRepository;
        _recoveryCodeRepository = recoveryCodeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Result.Failure<bool>("User not found");
        }

        // Verify password
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            _logger.LogWarning("Invalid password provided when disabling MFA for user: {UserId}", user.Id);
            return Result.Failure<bool>("Invalid password");
        }

        if (!user.MfaEnabled)
        {
            return Result.Failure<bool>("MFA is not enabled for this user");
        }

        // Remove MFA secrets
        await _mfaSecretRepository.DeleteByUserIdAsync(user.Id, cancellationToken);

        // Remove recovery codes
        await _recoveryCodeRepository.DeleteByUserIdAsync(user.Id, cancellationToken);

        // Disable MFA
        user.MfaEnabled = false;
        await _userManager.UpdateAsync(user);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MFA disabled for user: {UserId}", user.Id);

        return Result.Success(true);
    }
}
