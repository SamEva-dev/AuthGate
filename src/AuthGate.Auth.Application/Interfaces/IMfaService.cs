
namespace AuthGate.Auth.Application.Interfaces;

public interface IMfaService
{
    (string secret, string qrCodeUri) GenerateTOTP(string email);
    bool VerifyTOTP(string secret, string code);
}
