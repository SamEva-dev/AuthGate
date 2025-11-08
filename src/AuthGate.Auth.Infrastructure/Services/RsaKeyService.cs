using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Service for managing RSA keys for JWT signing (RS256)
/// </summary>
public class RsaKeyService
{
    private readonly RSA _rsa;
    private readonly string _keyId;
    private RsaSecurityKey? _securityKey;

    public RsaKeyService()
    {
        _rsa = RSA.Create(2048);
        _keyId = Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Get the RSA security key for signing
    /// </summary>
    public RsaSecurityKey GetSigningKey()
    {
        if (_securityKey == null)
        {
            _securityKey = new RsaSecurityKey(_rsa)
            {
                KeyId = _keyId
            };
        }
        return _securityKey;
    }

    /// <summary>
    /// Get the Key ID
    /// </summary>
    public string GetKeyId() => _keyId;

    /// <summary>
    /// Get RSA parameters for JWKS
    /// </summary>
    public RSAParameters GetPublicParameters() => _rsa.ExportParameters(false);

    /// <summary>
    /// Export public key as PEM (for external validation if needed)
    /// </summary>
    public string ExportPublicKeyPem()
    {
        var publicKey = _rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToBase64String(publicKey);
    }

    /// <summary>
    /// Import RSA key from configuration (if persisted)
    /// </summary>
    public void ImportKey(string base64PrivateKey)
    {
        var keyBytes = Convert.FromBase64String(base64PrivateKey);
        _rsa.ImportRSAPrivateKey(keyBytes, out _);
    }

    /// <summary>
    /// Export private key (for persistence - SECURE STORAGE ONLY!)
    /// </summary>
    public string ExportPrivateKey()
    {
        var privateKey = _rsa.ExportRSAPrivateKey();
        return Convert.ToBase64String(privateKey);
    }
}
