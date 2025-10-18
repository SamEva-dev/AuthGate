using AuthGate.Auth.Application.Interfaces;
using OtpNet;

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
}