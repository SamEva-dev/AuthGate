
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Interfaces;

public interface IMfaService
{
    (string secret, string qrCodeUri) GenerateTOTP(string email);
    bool VerifyTOTP(string secret, string code);

    /// <summary>
    /// Génère un secret TOTP + un QR code base64 compatible Google Authenticator.
    /// </summary>
    (string Secret, string QrCodeBase64) GenerateSetup(User user, string issuer);

    /// <summary>
    /// Vérifie un code TOTP à 6 chiffres saisi par l’utilisateur.
    /// </summary>
    bool VerifyCode(string secret, string code);
}
