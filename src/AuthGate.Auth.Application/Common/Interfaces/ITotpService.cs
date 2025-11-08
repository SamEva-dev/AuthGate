namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Service interface for Time-based One-Time Password (TOTP) operations
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Generates a new TOTP secret
    /// </summary>
    /// <returns>Base32-encoded secret</returns>
    string GenerateSecret();

    /// <summary>
    /// Generates a QR code URI for a TOTP secret
    /// </summary>
    /// <param name="email">User email</param>
    /// <param name="secret">TOTP secret</param>
    /// <param name="issuer">Issuer name (e.g., "AuthGate")</param>
    /// <returns>QR code URI</returns>
    string GenerateQrCodeUri(string email, string secret, string issuer);

    /// <summary>
    /// Verifies a TOTP code against a secret
    /// </summary>
    /// <param name="secret">TOTP secret</param>
    /// <param name="code">6-digit code to verify</param>
    /// <returns>True if code is valid, false otherwise</returns>
    bool VerifyCode(string secret, string code);

    /// <summary>
    /// Generates recovery codes for backup authentication
    /// </summary>
    /// <param name="count">Number of recovery codes to generate</param>
    /// <returns>List of recovery codes</returns>
    List<string> GenerateRecoveryCodes(int count = 10);
}
