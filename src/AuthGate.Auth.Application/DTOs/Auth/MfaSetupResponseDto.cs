namespace AuthGate.Auth.Application.DTOs.Auth;

/// <summary>
/// Response DTO for MFA setup
/// </summary>
public class MfaSetupResponseDto
{
    /// <summary>
    /// Secret key for TOTP
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// QR code data URI for scanning
    /// </summary>
    public required string QrCodeDataUri { get; set; }

    /// <summary>
    /// Manual entry key (formatted)
    /// </summary>
    public required string ManualEntryKey { get; set; }

    /// <summary>
    /// Recovery codes for backup access
    /// </summary>
    public List<string> RecoveryCodes { get; set; } = new();
}
