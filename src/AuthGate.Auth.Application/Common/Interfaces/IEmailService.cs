namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a password reset email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="resetToken">Password reset token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email verification email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="verificationToken">Email verification token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailVerificationAsync(string toEmail, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic email
    /// </summary>
    /// <param name="toEmail">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
