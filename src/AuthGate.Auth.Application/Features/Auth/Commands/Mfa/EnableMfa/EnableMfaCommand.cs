using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Mfa.EnableMfa;

/// <summary>
/// Command to enable MFA for a user
/// </summary>
public record EnableMfaCommand : IRequest<Result<MfaSetupResponseDto>>, IAuditableCommand
{
    /// <summary>
    /// User ID to enable MFA for
    /// </summary>
    public Guid UserId { get; init; }

    public AuditAction AuditAction => Domain.Enums.AuditAction.MfaEnabled;

    public string GetAuditDescription() => $"Enabled MFA for user {UserId}";
}
