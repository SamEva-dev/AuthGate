namespace AuthGate.Auth.Infrastructure.Services.Email;

public sealed class BrevoSettings
{
    // Mode: BREVO_API (default) or BREVO_SMTP
    public string Mode { get; set; } = "BREVO_API";

    // API
    public string ApiKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.brevo.com";
    public bool Sandbox { get; set; }

    // Sender
    public string SenderName { get; set; } = "LocaGuest";
    public string SenderEmail { get; set; } = string.Empty;

    // SMTP
    public string SmtpHost { get; set; } = "smtp-relay.brevo.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool SmtpUseTls { get; set; } = true;
}
