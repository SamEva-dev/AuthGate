using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using OtpNet;
using QRCoder;

namespace AuthGate.Auth.Infrastructure.Identity;

public class MfaService : IMfaService
{
    public (string secret, string qrCodeUri) GenerateTOTP(string email)
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(key);
        var otpUrl = $"otpauth://totp/AuthGate:{email}?secret={secret}&issuer=AuthGate&digits=6&period=30";
        return (secret, otpUrl);
    }

    public bool VerifyTOTP(string secret, string code)
    {
        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key);
        return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
    }

    public (string Secret, string QrCodeBase64) GenerateSetup(User user, string issuer)
    {
        // 1️⃣ Générer un secret aléatoire (base32)
        var secret = KeyGeneration.GenerateRandomKey(20);
        var secretBase32 = Base32Encoding.ToString(secret);

        // 2️⃣ URI compatible Google Authenticator
        var uri = new OtpUri(OtpType.Totp, secretBase32, user.Email, issuer);
        var otpauth = uri.ToString();

        // 3️⃣ QR code
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpauth, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrBytes = qrCode.GetGraphic(20);
        var qrBase64 = Convert.ToBase64String(qrBytes);

        return (secretBase32, $"data:image/png;base64,{qrBase64}");
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(2, 2)); // ±2 intervals
    }
}