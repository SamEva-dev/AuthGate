using MediatR;

namespace AuthGate.Auth.Application.Features.TwoFactor.Commands.DisableTwoFactor;

/// <summary>
/// Command to disable 2FA for the current user
/// </summary>
public class DisableTwoFactorCommand : IRequest<bool>
{
    public string Password { get; set; } = string.Empty; // For identity confirmation
}
