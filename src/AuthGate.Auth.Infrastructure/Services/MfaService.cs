
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using OtpNet;
using QRCoder;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Service MFA TOTP compatible Google Authenticator / Authy.
/// </summary>
public class MfaService : IMfaService
{
    public (string secret, string qrCodeUri) GenerateTOTP(string email)
    {
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(secretKey);

        var issuer = "AuthGate";
        var otpauth = $"otpauth://totp/{issuer}:{email}?secret={secretBase32}&issuer={issuer}";

        return (secretBase32, otpauth);
    }

    public bool VerifyTOTP(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        // 1️⃣ Convertit le secret Base32 en bytes
        var bytes = Base32Encoding.ToBytes(secret);

        // 2️⃣ Calcule le TOTP courant (30 s par défaut)
        var totp = new Totp(bytes, step: 30, totpSize: 6);

        // 3️⃣ Vérifie le code dans une fenêtre ±2 intervalles
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(2, 2));
    }

    public (string Secret, string QrCodeBase64) GenerateSetup(User user, string issuer)
    {
        // 1️⃣ Génère une clé aléatoire (160 bits)
        var keyBytes = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(keyBytes);

        // 2️⃣ Crée l’URI otpauth:// utilisé par Google Authenticator
        // Format standard : otpauth://totp/{issuer}:{email}?secret={secret}&issuer={issuer}
        var otpauth = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(user.Email)}" +
                      $"?secret={secretBase32}&issuer={Uri.EscapeDataString(issuer)}";

        // 3️⃣ Génère le QR code PNG encodé en Base64
        using var qrGen = new QRCodeGenerator();
        var qrData = qrGen.CreateQrCode(otpauth, QRCodeGenerator.ECCLevel.Q);
        using var qrPng = new PngByteQRCode(qrData);
        var pngBytes = qrPng.GetGraphic(20);
        var base64 = Convert.ToBase64String(pngBytes);

        return (secretBase32, $"data:image/png;base64,{base64}");
    }

    public bool VerifyCode(string secret, string code) => VerifyTOTP(secret, code);
}