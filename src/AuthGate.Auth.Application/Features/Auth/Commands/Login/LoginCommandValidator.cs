using FluentValidation;
using System;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Login;

/// <summary>
/// Validator for LoginCommand
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.App)
            .Must(app => string.IsNullOrWhiteSpace(app)
                         || string.Equals(app, "locaguest", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(app, "manager", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Invalid app. Must be locaguest or manager");
    }
}
