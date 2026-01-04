using AuthGate.Auth.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for JWKS (JSON Web Key Set) endpoint
/// </summary>
[ApiController]
[Route(".well-known")]
[AllowAnonymous]
public class JwksController : ControllerBase
{
    private readonly RsaKeyService _rsaKeyService;

    public JwksController(RsaKeyService rsaKeyService)
    {
        _rsaKeyService = rsaKeyService;
    }

    /// <summary>
    /// Get JSON Web Key Set (JWKS) for JWT signature validation
    /// </summary>
    /// <returns>JWKS containing public keys</returns>
    [HttpGet("jwks.json")]
    [ProducesResponseType(typeof(JwksResponse), StatusCodes.Status200OK)]
    public IActionResult GetJwks()
    {
        var publicKeys = _rsaKeyService.GetAllPublicParameters();

        var jwks = publicKeys
            .Select(k => new JsonWebKey
            {
                Kty = "RSA",
                Use = "sig",
                Kid = k.Kid,
                Alg = "RS256",
                N = Base64UrlEncode(k.PublicParameters.Modulus!),
                E = Base64UrlEncode(k.PublicParameters.Exponent!)
            })
            .ToArray();

        var response = new JwksResponse { Keys = jwks };

        return Ok(response);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        // Convert to base64url
        base64 = base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return base64;
    }
}

/// <summary>
/// JWKS response model
/// </summary>
public class JwksResponse
{
    public JsonWebKey[] Keys { get; set; } = Array.Empty<JsonWebKey>();
}

/// <summary>
/// JSON Web Key model
/// </summary>
public class JsonWebKey
{
    public string Kty { get; set; } = string.Empty; // Key Type
    public string Use { get; set; } = string.Empty; // Public Key Use
    public string Kid { get; set; } = string.Empty; // Key ID
    public string Alg { get; set; } = string.Empty; // Algorithm
    public string N { get; set; } = string.Empty;   // Modulus
    public string E { get; set; } = string.Empty;   // Exponent
}
