using AuthGate.Auth.Application.Services.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;

namespace AuthGate.Auth.Infrastructure.Services.Email;

public sealed class BrevoEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly BrevoSettings _brevo;
    private readonly ILogger<BrevoEmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public BrevoEmailService(
        IOptions<EmailSettings> settings,
        IOptions<BrevoSettings> brevoSettings,
        ILogger<BrevoEmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _brevo = brevoSettings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public Task SendInvitationEmailAsync(
        string toEmail,
        string toName,
        string inviterName,
        string organizationName,
        string role,
        string invitationUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Invitation à rejoindre {organizationName} sur LocaGuest";

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
            <p><strong>{inviterName}</strong> vous invite à rejoindre <strong>{organizationName}</strong> sur LocaGuest.</p>
            
            <div class=""info-box"">
                <p><strong>Votre rôle:</strong> {role}</p>
                <p><strong>Organisation:</strong> {organizationName}</p>
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

{inviterName} vous invite à rejoindre {organizationName} sur LocaGuest.

Votre rôle: {role}
Organisation: {organizationName}

Pour accepter cette invitation, cliquez sur le lien suivant:
{invitationUrl}

Cette invitation expire le {expiresAt:dd/MM/yyyy à HH:mm}

Si vous n'avez pas demandé cette invitation, vous pouvez ignorer cet email en toute sécurité.

---
© 2025 LocaGuest. Tous droits réservés.
";

        return SendEmailAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
    }

    public Task SendWelcomeEmailAsync(
        string toEmail,
        string firstName,
        string organizationName,
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
            <p>Nous sommes ravis de vous accueillir dans <strong>{organizationName}</strong>!</p>

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

        return SendEmailAsync(toEmail, subject, htmlBody, null, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(
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

        return SendEmailAsync(toEmail, subject, htmlBody, null, cancellationToken);
    }

    public Task SendEmailVerificationAsync(
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

        return SendEmailAsync(toEmail, subject, htmlBody, null, cancellationToken);
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
            _logger.LogDebug(
                "Email body (first 200 chars): {Body}",
                htmlBody.Length > 200 ? htmlBody[..200] + "..." : htmlBody);
            return;
        }

        var mode = (_brevo.Mode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(mode))
            mode = "BREVO_API";

        if (string.Equals(mode, "BREVO_SMTP", StringComparison.OrdinalIgnoreCase))
        {
            await SendViaSmtpAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
            return;
        }

        await SendViaApiAsync(toEmail, subject, htmlBody, textBody, cancellationToken);
    }

    private async Task SendViaApiAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_brevo.ApiKey))
            throw new InvalidOperationException("Brevo:ApiKey is required when EmailSettings:Provider=Brevo and Brevo:Mode=BREVO_API");

        var client = _httpClientFactory.CreateClient("BrevoEmail");

        using var req = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
        req.Headers.TryAddWithoutValidation("api-key", _brevo.ApiKey);

        var senderEmail = !string.IsNullOrWhiteSpace(_brevo.SenderEmail) ? _brevo.SenderEmail : _settings.FromEmail;
        var senderName = !string.IsNullOrWhiteSpace(_brevo.SenderName) ? _brevo.SenderName : _settings.FromName;

        var payload = new BrevoSendEmailRequest
        {
            Sender = new BrevoSender { Email = senderEmail, Name = senderName },
            To = new List<BrevoRecipient> { new() { Email = toEmail } },
            Subject = subject,
            HtmlContent = htmlBody,
            TextContent = textBody,
        };

        if (_brevo.Sandbox)
        {
            payload.Tags.Add("sandbox");
        }

        req.Content = JsonContent.Create(payload);

        using var resp = await client.SendAsync(req, cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Brevo API error ({(int)resp.StatusCode}): {err}");
        }

        _logger.LogInformation("Email sent successfully via Brevo API to {Email}: {Subject}", toEmail, subject);
    }

    private async Task SendViaSmtpAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_brevo.SmtpUsername) || string.IsNullOrWhiteSpace(_brevo.SmtpPassword))
            throw new InvalidOperationException("Brevo SMTP credentials are required when Brevo:Mode=BREVO_SMTP");

        using var message = new MailMessage
        {
            From = new MailAddress(
                !string.IsNullOrWhiteSpace(_brevo.SenderEmail) ? _brevo.SenderEmail : _settings.FromEmail,
                !string.IsNullOrWhiteSpace(_brevo.SenderName) ? _brevo.SenderName : _settings.FromName),
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

        using var smtpClient = new SmtpClient(_brevo.SmtpHost, _brevo.SmtpPort)
        {
            EnableSsl = _brevo.SmtpUseTls,
            Credentials = new NetworkCredential(_brevo.SmtpUsername, _brevo.SmtpPassword)
        };

        await smtpClient.SendMailAsync(message, cancellationToken);

        _logger.LogInformation("Email sent successfully via Brevo SMTP to {Email}: {Subject}", toEmail, subject);
    }

    private sealed class BrevoSendEmailRequest
    {
        public BrevoSender Sender { get; set; } = new();
        public List<BrevoRecipient> To { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }
        public string? TextContent { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    private sealed class BrevoSender
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class BrevoRecipient
    {
        public string Email { get; set; } = string.Empty;
    }
}
