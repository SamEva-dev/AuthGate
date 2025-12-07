using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using FluentValidation;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.VerifyRecoveryCode;

/// <summary>
/// Command for verifying a 2FA recovery code and completing login
/// </summary>
public class VerifyRecoveryCodeCommand : IRequest<Result<LoginResponseDto>>
{
    /// <summary>
    /// Gets or sets the temporary MFA token from initial login
    /// </summary>
    public required string MfaToken { get; set; }

    /// <summary>
    /// Gets or sets the recovery code (8-12 characters alphanumeric)
    /// </summary>
    public required string RecoveryCode { get; set; }
}

/// <summary>
/// Validator for VerifyRecoveryCodeCommand
/// </summary>
public class VerifyRecoveryCodeCommandValidator : AbstractValidator<VerifyRecoveryCodeCommand>
{
    public VerifyRecoveryCodeCommandValidator()
    {
        RuleFor(x => x.MfaToken)
            .NotEmpty()
            .WithMessage("MFA token is required");

        RuleFor(x => x.RecoveryCode)
            .NotEmpty()
            .WithMessage("Recovery code is required")
            .MinimumLength(8)
            .WithMessage("Recovery code must be at least 8 characters")
            .MaximumLength(12)
            .WithMessage("Recovery code must not exceed 12 characters");
    }
}
