using AuthGate.Auth.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AuthGate.Auth.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        var resetUrl = $"{_configuration["Frontend:Url"]}/reset-password?token={resetToken}";
        
        var body = $@"
            <h2>Password Reset Request</h2>
            <p>You have requested to reset your password. Please click the link below to proceed:</p>
            <p><a href=""{resetUrl}"">Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you did not request this, please ignore this email.</p>
        ";

        await SendEmailAsync(toEmail, "Password Reset Request", body, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(string toEmail, string verificationToken, CancellationToken cancellationToken = default)
    {
        var verificationUrl = $"{_configuration["Frontend:Url"]}/verify-email?token={verificationToken}";
        
        var body = $@"
            <h2>Email Verification</h2>
            <p>Please verify your email address by clicking the link below:</p>
            <p><a href=""{verificationUrl}"">Verify Email</a></p>
            <p>If you did not create an account, please ignore this email.</p>
        ";

        await SendEmailAsync(toEmail, "Email Verification", body, cancellationToken);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                _configuration["Email:FromName"] ?? "AuthGate",
                _configuration["Email:FromAddress"] ?? "noreply@authgate.com"
            ));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            var host = _configuration["Email:SmtpHost"] ?? "localhost";
            var port = int.Parse(_configuration["Email:SmtpPort"] ?? "1025"); // MailHog default port
            
            await client.ConnectAsync(host, port, false, cancellationToken);
            
            // MailHog doesn't require authentication, but support it if configured
            var username = _configuration["Email:SmtpUsername"];
            var password = _configuration["Email:SmtpPassword"];
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
}
