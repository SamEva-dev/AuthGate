namespace AuthGate.Auth.Application.Services.Email;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send invitation email to a collaborator
    /// </summary>
    Task SendInvitationEmailAsync(
        string toEmail,
        string toName,
        string inviterName,
        string organizationName,
        string role,
        string invitationUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send welcome email after registration
    /// </summary>
    Task SendWelcomeEmailAsync(
        string toEmail,
        string firstName,
        string organizationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string firstName,
        string resetUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email verification
    /// </summary>
    Task SendEmailVerificationAsync(
        string toEmail,
        string firstName,
        string verificationUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send generic email
    /// </summary>
    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken cancellationToken = default);
}
