namespace AuthGate.Auth.Application.Services.Email;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// Email provider: SMTP or SendGrid
    /// </summary>
    public string Provider { get; set; } = "Brevo";

    /// <summary>
    /// Sender email address
    /// </summary>
    public string FromEmail { get; set; } = "noreply@locaguest.com";

    /// <summary>
    /// Sender display name
    /// </summary>
    public string FromName { get; set; } = "LocaGuest";

    /// <summary>
    /// SMTP server host
    /// </summary>
    public string? SmtpHost { get; set; }

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Use SSL for SMTP
    /// </summary>
    public bool SmtpUseSsl { get; set; } = true;

    /// <summary>
    /// SendGrid API key
    /// </summary>
    public string? SendGridApiKey { get; set; }

    /// <summary>
    /// Base URL for the frontend application
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    /// <summary>
    /// Enable email sending (false = console logging only)
    /// </summary>
    public bool EnableSending { get; set; } = true;
}
