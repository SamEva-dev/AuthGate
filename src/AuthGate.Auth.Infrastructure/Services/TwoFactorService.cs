using AuthGate.Auth.Application.Services;
using OtpNet;
using QRCoder;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Implementation of Two-Factor Authentication (TOTP) service
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    public string GenerateSecret()
    {
        // Génère un secret de 20 bytes (160 bits) - Recommandé par RFC 4226
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUri(string secret, string userEmail, string issuer = "LocaGuest")
    {
        // Format: otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(userEmail);
        
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
    }

    public string GenerateQrCodeImage(string qrCodeUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        
        var qrCodeBytes = qrCode.GetGraphic(20); // 20 pixels per module
        return Convert.ToBase64String(qrCodeBytes);
    }

    public bool ValidateCode(string secret, string code)
    {
        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);
            
            // Valide le code avec une fenêtre de tolérance de 1 (30s avant/après)
            return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
        catch
        {
            return false;
        }
    }

    public List<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>();
        var random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            // Génère des codes de 8 caractères alphanumériques
            var code = new string(Enumerable.Range(0, 8)
                .Select(_ => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[random.Next(32)])
                .ToArray());
            
            // Format: XXXX-XXXX
            codes.Add($"{code.Substring(0, 4)}-{code.Substring(4, 4)}");
        }
        
        return codes;
    }
}
