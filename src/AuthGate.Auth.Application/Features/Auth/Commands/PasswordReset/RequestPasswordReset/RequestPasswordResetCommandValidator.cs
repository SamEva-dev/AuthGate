using FluentValidation;

namespace AuthGate.Auth.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;

/// <summary>
/// Validator for RequestPasswordResetCommand
/// </summary>
public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");
    }
}
