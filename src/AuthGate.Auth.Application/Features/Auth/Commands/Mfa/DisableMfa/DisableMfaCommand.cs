using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Mfa.DisableMfa;

/// <summary>
/// Command to disable MFA for a user
/// </summary>
public record DisableMfaCommand : IRequest<Result<bool>>, IAuditableCommand
{
    /// <summary>
    /// User ID to disable MFA for
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Current password for verification
    /// </summary>
    public required string Password { get; init; }

    public AuditAction AuditAction => Domain.Enums.AuditAction.MfaDisabled;

    public string GetAuditDescription() => $"Disabled MFA for user {UserId}";
}
