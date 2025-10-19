using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Infrastructure.Identity;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _config;

    public EmailService(ILogger<EmailService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task SendValidationEmailAsync(string to, string token)
    {
        _logger.LogInformation("SendValidationEmail -> to:{To}, token:{Token}", to, token);
        return Task.CompletedTask;
    }

    public async Task SendResetPasswordAsync(string toEmail, string token)
    {
        var frontendUrl = _config["Frontend:ResetPasswordUrl"]
            ?? "https://localhost:4200/reset-password";

        // Génération du lien complet
        var resetLink = $"{frontendUrl}?token={token}";

        // Simule un envoi (dev)
        _logger.LogInformation("📧 [EmailService] Password reset email for {Email}", toEmail);
        _logger.LogInformation("🔗 Reset link: {Link}", resetLink);

        // Ici, tu peux ajouter un vrai envoi plus tard (SMTP, SendGrid, etc.)
        await Task.CompletedTask;
    }

    public async Task SendPasswordResetAsync(string toEmail, string token)
    {
        // URL du frontend de reset (configurable dans appsettings)
        var frontendUrl = _config["Frontend:ResetPasswordUrl"]
            ?? "https://localhost:4200/reset-password";

        // Génération du lien complet
        var resetLink = $"{frontendUrl}?token={token}";

        // Simule un envoi (dev)
        _logger.LogInformation("📧 [EmailService] Password reset email for {Email}", toEmail);
        _logger.LogInformation("🔗 Reset link: {Link}", resetLink);

        // Ici, tu peux ajouter un vrai envoi plus tard (SMTP, SendGrid, etc.)
        await Task.CompletedTask;
    }
}