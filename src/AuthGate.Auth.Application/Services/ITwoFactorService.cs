namespace AuthGate.Auth.Application.Services;

/// <summary>
/// Service for Two-Factor Authentication (TOTP) operations
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Génère un nouveau secret TOTP (Base32)
    /// </summary>
    string GenerateSecret();

    /// <summary>
    /// Génère l'URI pour le QR code (otpauth://)
    /// </summary>
    string GenerateQrCodeUri(string secret, string userEmail, string issuer = "LocaGuest");

    /// <summary>
    /// Génère l'image QR code en Base64
    /// </summary>
    string GenerateQrCodeImage(string qrCodeUri);

    /// <summary>
    /// Valide un code TOTP (6 digits)
    /// </summary>
    bool ValidateCode(string secret, string code);

    /// <summary>
    /// Génère des recovery codes sécurisés
    /// </summary>
    List<string> GenerateRecoveryCodes(int count = 10);
}
