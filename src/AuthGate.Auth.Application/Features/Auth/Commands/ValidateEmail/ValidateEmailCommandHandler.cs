using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ValidateEmail;

public sealed class ValidateEmailCommandHandler : IRequestHandler<ValidateEmailCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ValidateEmailCommandHandler> _logger;

    public ValidateEmailCommandHandler(UserManager<User> userManager, ILogger<ValidateEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ValidateEmailCommand request, CancellationToken cancellationToken)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<bool>("Email is required");

        var token = request.Token ?? string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<bool>("Token is required");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return Result.Failure<bool>("Invalid token");

        if (user.EmailConfirmed)
            return Result.Success(true);

        var res = await _userManager.ConfirmEmailAsync(user, token);
        if (!res.Succeeded)
        {
            var err = string.Join(", ", res.Errors.Select(e => e.Description));
            _logger.LogWarning("Email confirmation failed for {Email}: {Errors}", email, err);
            return Result.Failure<bool>("Invalid token");
        }

        _logger.LogInformation("Email confirmed for {Email}", email);
        return Result.Success(true);
    }
}
