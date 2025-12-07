using MediatR;

namespace AuthGate.Auth.Application.Features.TwoFactor.Commands.VerifyAndEnableTwoFactor;

/// <summary>
/// Command to verify TOTP code and enable 2FA
/// </summary>
public class VerifyAndEnableTwoFactorCommand : IRequest<bool>
{
    public string Code { get; set; } = string.Empty; // 6 digits
}
