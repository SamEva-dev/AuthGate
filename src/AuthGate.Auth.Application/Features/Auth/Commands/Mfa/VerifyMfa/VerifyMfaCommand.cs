using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Mfa.VerifyMfa;

/// <summary>
/// Command to verify and activate MFA
/// </summary>
public record VerifyMfaCommand : IRequest<Result<bool>>, IAuditableCommand
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// TOTP secret (provided during setup)
    /// </summary>
    public required string Secret { get; init; }

    /// <summary>
    /// TOTP code from authenticator app
    /// </summary>
    public required string Code { get; init; }

    public AuditAction AuditAction => Domain.Enums.AuditAction.MfaVerified;

    public string GetAuditDescription() => $"Verified MFA for user {UserId}";
}
