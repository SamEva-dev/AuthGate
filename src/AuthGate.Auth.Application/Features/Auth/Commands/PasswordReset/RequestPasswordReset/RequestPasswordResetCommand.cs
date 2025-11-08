using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;

/// <summary>
/// Command to request a password reset
/// </summary>
public record RequestPasswordResetCommand : IRequest<Result<bool>>, IAuditableCommand
{
    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; init; }

    public AuditAction AuditAction => AuditAction.PasswordResetRequested;

    public string GetAuditDescription() => $"Password reset requested for email {Email}";
}
