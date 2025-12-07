using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Verify2FA;

/// <summary>
/// Command to verify 2FA/TOTP code and complete login
/// </summary>
public record Verify2FACommand : IRequest<Result<LoginResponseDto>>
{
    /// <summary>
    /// Temporary MFA token from login response
    /// </summary>
    public required string MfaToken { get; init; }

    /// <summary>
    /// 6-digit TOTP code from authenticator app
    /// </summary>
    public required string Code { get; init; }
}
