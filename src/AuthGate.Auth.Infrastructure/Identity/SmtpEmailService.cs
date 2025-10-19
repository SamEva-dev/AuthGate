

using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;

namespace AuthGate.Auth.Infrastructure.Identity;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string token)
    {
        var frontendUrl = _config["Frontend:ResetPasswordUrl"]
            ?? "https://localhost:4200/reset-password";
        var resetLink = $"{frontendUrl}?token={token}";

        var smtpSection = _config.GetSection("Smtp");
        var host = smtpSection["Host"];
        var port = int.Parse(smtpSection["Port"] ?? "587");
        var user = smtpSection["User"];
        var pass = smtpSection["Pass"];
        var from = smtpSection["From"] ?? user;

        var message = new MailMessage
        {
            From = new MailAddress(from, "AuthGate"),
            Subject = "Réinitialisation de votre mot de passe",
            Body = $"""
Bonjour,

Vous avez demandé à réinitialiser votre mot de passe.
Veuillez cliquer sur le lien ci-dessous pour continuer :

{resetLink}

Si vous n'êtes pas à l'origine de cette demande, ignorez simplement cet e-mail.

Cordialement,
L'équipe AuthGate
""",
            IsBodyHtml = false
        };

        message.To.Add(toEmail);

        using var smtp = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        await smtp.SendMailAsync(message);
        _logger.LogInformation("📧 Password reset email sent to {Email}", toEmail);
    }

    public Task SendResetPasswordAsync(string to, string token)
    {
        throw new NotImplementedException();
    }

    public Task SendValidationEmailAsync(string to, string token)
    {
        throw new NotImplementedException();
    }
}