using AuthGate.Auth.Application.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AuthGate.Auth.Infrastructure.Services.Email;

/// <summary>
/// SMTP email service implementation
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailSettings> settings,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendInvitationEmailAsync(
        string toEmail,
        string toName,
        string inviterName,
        string tenantName,
        string role,
        string invitationUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Invitation à rejoindre {tenantName} sur LocaGuest";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; padding: 12px 24px; background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .info-box {{ background: white; border-left: 4px solid #10b981; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #6b7280; font-size: 14px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🏡 LocaGuest</h1>
            <p>Gestion immobilière simplifiée</p>
        </div>
        <div class=""content"">
            <h2>Bonjour,</h2>
            <p><strong>{inviterName}</strong> vous invite à rejoindre <strong>{tenantName}</strong> sur LocaGuest.</p>
            
            <div class=""info-box"">
                <p><strong>Votre rôle:</strong> {role}</p>
                <p><strong>Organisation:</strong> {tenantName}</p>
            </div>

            <p>En acceptant cette invitation, vous pourrez accéder à la plateforme et collaborer avec votre équipe pour gérer vos biens immobiliers.</p>

            <div style=""text-align: center;"">
                <a href=""{invitationUrl}"" class=""button"">Accepter l'invitation</a>
            </div>

            <p style=""font-size: 14px; color: #6b7280; margin-top: 20px;"">
                Cette invitation expire le <strong>{expiresAt:dd/MM/yyyy à HH:mm}</strong>
            </p>

            <p style=""font-size: 12px; color: #9ca3af; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb;"">
                Si vous n'avez pas demandé cette invitation, vous pouvez ignorer cet email en toute sécurité.
            </p>
        </div>
        <div class=""footer"">
            <p>© 2025 LocaGuest. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

        var textBody = $@"
Bonjour,

{inviterName} vous invite à rejoindre {tenantName} sur LocaGuest.

Votre rôle: {role}
Organisation: {tenantName}

Pour accepter cette invitation, cliquez sur le lien suivant:
{invitationUrl}

Cette invitation expire le {expiresAt:dd/MM/yyyy à HH:mm}

Si vous n'avez pas demandé cette invitation, vous pouvez ignorer cet email en toute sécurité.

---
© 2025 LocaGuest. Tous droits réservés.
";

        await SendEmailAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string firstName,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Bienvenue sur LocaGuest! 🎉";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; padding: 12px 24px; background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .feature {{ background: white; padding: 15px; margin: 10px 0; border-radius: 6px; border-left: 4px solid #10b981; }}
        .footer {{ text-align: center; margin-top: 30px; color: #6b7280; font-size: 14px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎉 Bienvenue sur LocaGuest!</h1>
        </div>
        <div class=""content"">
            <h2>Bonjour {firstName},</h2>
            <p>Nous sommes ravis de vous accueillir dans <strong>{tenantName}</strong>!</p>

            <p>LocaGuest est votre plateforme de gestion immobilière complète. Voici ce que vous pouvez faire:</p>

            <div class=""feature"">
                <strong>🏘️ Gérer vos biens</strong><br>
                Centralisez toutes les informations de vos propriétés
            </div>

            <div class=""feature"">
                <strong>👥 Suivre vos locataires</strong><br>
                Gérez les contrats et paiements en toute simplicité
            </div>

            <div class=""feature"">
                <strong>📊 Analyser la rentabilité</strong><br>
                Simulez et optimisez vos investissements
            </div>

            <div style=""text-align: center;"">
                <a href=""{_settings.FrontendBaseUrl}/app/dashboard"" class=""button"">Découvrir la plateforme</a>
            </div>

            <p>Si vous avez des questions, n'hésitez pas à nous contacter.</p>
        </div>
        <div class=""footer"">
            <p>© 2025 LocaGuest. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, htmlBody, null, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string firstName,
        string resetUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var subject = "Réinitialisation de votre mot de passe LocaGuest";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; padding: 12px 24px; background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .warning {{ background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #6b7280; font-size: 14px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔒 Réinitialisation de mot de passe</h1>
        </div>
        <div class=""content"">
            <h2>Bonjour {firstName},</h2>
            <p>Vous avez demandé à réinitialiser votre mot de passe LocaGuest.</p>

            <div style=""text-align: center;"">
                <a href=""{resetUrl}"" class=""button"">Réinitialiser mon mot de passe</a>
            </div>

            <p style=""font-size: 14px; color: #6b7280; margin-top: 20px;"">
                Ce lien expire le <strong>{expiresAt:dd/MM/yyyy à HH:mm}</strong>
            </p>

            <div class=""warning"">
                <strong>⚠️ Attention:</strong> Si vous n'avez pas demandé cette réinitialisation, ignorez cet email. Votre mot de passe actuel restera inchangé.
            </div>
        </div>
        <div class=""footer"">
            <p>© 2025 LocaGuest. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, htmlBody, null, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(
        string toEmail,
        string firstName,
        string verificationUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "Vérifiez votre adresse email";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; padding: 12px 24px; background: linear-gradient(135deg, #10b981 0%, #14b8a6 100%); color: white; text-decoration: none; border-radius: 6px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #6b7280; font-size: 14px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✉️ Vérification d'email</h1>
        </div>
        <div class=""content"">
            <h2>Bonjour {firstName},</h2>
            <p>Merci de vous être inscrit sur LocaGuest!</p>
            <p>Pour finaliser votre inscription, veuillez vérifier votre adresse email en cliquant sur le bouton ci-dessous:</p>

            <div style=""text-align: center;"">
                <a href=""{verificationUrl}"" class=""button"">Vérifier mon email</a>
            </div>
        </div>
        <div class=""footer"">
            <p>© 2025 LocaGuest. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, htmlBody, null, cancellationToken);
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableSending)
        {
            _logger.LogInformation(
                "Email sending disabled. Would send to {Email}: {Subject}",
                toEmail, subject);
            _logger.LogDebug("Email body (first 200 chars): {Body}", 
                htmlBody.Length > 200 ? htmlBody[..200] + "..." : htmlBody);
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail));

            if (!string.IsNullOrEmpty(textBody))
            {
                message.AlternateViews.Add(
                    AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain"));
            }

            using var smtpClient = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpUseSsl,
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword)
            };

            await smtpClient.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", toEmail, subject);
            throw;
        }
    }
}
