using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AuthGate.Auth.Application.Features.TwoFactor.Commands.DisableTwoFactor;

/// <summary>
/// Handler for disabling 2FA
/// </summary>
public class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, bool>
{
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DisableTwoFactorCommandHandler> _logger;
    // TODO: Inject UserManager<User> for password validation

    public DisableTwoFactorCommandHandler(
        IMfaSecretRepository mfaSecretRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DisableTwoFactorCommandHandler> logger)
    {
        _mfaSecretRepository = mfaSecretRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        // Get current user ID from JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // TODO: Validate password with UserManager
        // var user = await _userManager.FindByIdAsync(userId.ToString());
        // var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        // if (!passwordValid) throw new InvalidOperationException("Invalid password");

        // Get MFA secret
        var mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(userId, cancellationToken);
        if (mfaSecret == null || !mfaSecret.IsEnabled)
        {
            throw new InvalidOperationException("Two-factor authentication is not enabled");
        }

        // Disable 2FA
        mfaSecret.Disable();
        _mfaSecretRepository.Update(mfaSecret);

        _logger.LogInformation("2FA disabled for user {UserId}", userId);

        return true;
    }
}
