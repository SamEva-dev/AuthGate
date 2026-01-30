using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using LocaGuest.Emailing.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ResendConfirmEmail;

public class ResendConfirmEmailCommandHandler : IRequestHandler<ResendConfirmEmailCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailingService _emailing;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResendConfirmEmailCommandHandler> _logger;

    public ResendConfirmEmailCommandHandler(
        UserManager<User> userManager,
        IEmailingService emailing,
        IConfiguration configuration,
        ILogger<ResendConfirmEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _emailing = emailing;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ResendConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<bool>("Email is required.");

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Success(true);
        }

        if (user.EmailConfirmed)
        {
            return Result.Success(true);
        }

        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var frontendUrl = _configuration["Frontend:ConfirmEmailUrl"] ?? "http://localhost:4200/confirm-email";
        var verifyUrl = $"{frontendUrl}?token={Uri.EscapeDataString(confirmToken)}&email={Uri.EscapeDataString(user.Email!)}";

        var firstName = user.FirstName ?? string.Empty;
        var subject = "Vérifiez votre adresse email";
        var htmlBody = $$"""
<h2>✉️ Vérification d'email</h2>
<p>Bonjour {{firstName}},</p>
<p>Pour finaliser votre inscription, veuillez vérifier votre adresse email en cliquant sur le bouton ci-dessous :</p>
<p><a href="{{verifyUrl}}">Vérifier mon email</a></p>
<p>Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>
""";

        try
        {
            await _emailing.QueueHtmlAsync(
                toEmail: user.Email!,
                subject: subject,
                htmlContent: htmlBody,
                textContent: null,
                attachments: null,
                tags: EmailUseCaseTags.AuthConfirmEmail,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to queue confirm email for {Email}", email);
        }

        return Result.Success(true);
    }
}
