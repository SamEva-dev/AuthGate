using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.PasswordReset.ResetPassword;

/// <summary>
/// Command to reset a user's password
/// </summary>
public record ResetPasswordCommand : IRequest<Result<bool>>, IAuditableCommand
{
    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Password reset token
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// New password
    /// </summary>
    public required string NewPassword { get; init; }

    /// <summary>
    /// Confirm new password
    /// </summary>
    public required string ConfirmPassword { get; init; }

    public AuditAction AuditAction => AuditAction.PasswordReset;

    public string GetAuditDescription() => $"Password reset for email {Email}";
}
