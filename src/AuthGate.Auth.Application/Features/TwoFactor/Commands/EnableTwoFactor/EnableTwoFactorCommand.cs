using MediatR;

namespace AuthGate.Auth.Application.Features.TwoFactor.Commands.EnableTwoFactor;

/// <summary>
/// Command to initialize 2FA for the current user
/// </summary>
public class EnableTwoFactorCommand : IRequest<EnableTwoFactorResponse>
{
    // Uses current authenticated user from context
}

/// <summary>
/// Response containing QR code and recovery codes for 2FA setup
/// </summary>
public class EnableTwoFactorResponse
{
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUri { get; set; } = string.Empty;
    public string QrCodeImage { get; set; } = string.Empty; // Base64
    public List<string> RecoveryCodes { get; set; } = new();
}
