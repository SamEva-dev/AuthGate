using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Linq;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Service for managing RSA keys for JWT signing (RS256)
/// </summary>
public class RsaKeyService
{
    private readonly object _lock = new();
    private readonly IConfiguration _configuration;

    private readonly string _keystorePath;

    private readonly ConcurrentDictionary<string, RSA> _rsaCache = new(StringComparer.Ordinal);
    private RsaSecurityKey? _activeSecurityKey;
    private Keystore _keystore = new();

    public RsaKeyService(IConfiguration configuration)
    {
        _configuration = configuration;

        var home = Environment.GetEnvironmentVariable("AUTHGATE_HOME");
        if (string.IsNullOrWhiteSpace(home))
        {
            home = AppContext.BaseDirectory;
        }

        _keystorePath = Path.Combine(home, "keys", "jwks-rsa.json");

        LoadOrCreateKeystore();
        RotateIfNeeded();
        PruneIfNeeded();
        PersistKeystore();
    }

    /// <summary>
    /// Get the RSA security key for signing
    /// </summary>
    public RsaSecurityKey GetSigningKey()
    {
        lock (_lock)
        {
            if (_activeSecurityKey != null)
            {
                return _activeSecurityKey;
            }

            var kid = _keystore.ActiveKid;
            if (string.IsNullOrWhiteSpace(kid))
            {
                throw new InvalidOperationException("RSA keystore has no active kid.");
            }

            var rsa = GetOrCreateRsa(kid);
            _activeSecurityKey = new RsaSecurityKey(rsa) { KeyId = kid };
            return _activeSecurityKey;
        }
    }

    /// <summary>
    /// Get the Key ID
    /// </summary>
    public string GetKeyId() => _keystore.ActiveKid;

    /// <summary>
    /// Get RSA parameters for JWKS
    /// </summary>
    public RSAParameters GetPublicParameters()
    {
        var rsa = GetOrCreateRsa(_keystore.ActiveKid);
        return rsa.ExportParameters(false);
    }

    public IReadOnlyList<(string Kid, RSAParameters PublicParameters)> GetAllPublicParameters()
    {
        lock (_lock)
        {
            var result = new List<(string Kid, RSAParameters PublicParameters)>();
            foreach (var key in _keystore.Keys)
            {
                var rsa = GetOrCreateRsa(key.Kid);
                result.Add((key.Kid, rsa.ExportParameters(false)));
            }
            return result;
        }
    }

    /// <summary>
    /// Export public key as PEM (for external validation if needed)
    /// </summary>
    public string ExportPublicKeyPem()
    {
        var rsa = GetOrCreateRsa(_keystore.ActiveKid);
        var publicKey = rsa.ExportSubjectPublicKeyInfo();
        return Convert.ToBase64String(publicKey);
    }

    /// <summary>
    /// Import RSA key from configuration (if persisted)
    /// </summary>
    public void ImportKey(string base64PrivateKey)
    {
        throw new NotSupportedException("ImportKey is not supported in keystore mode.");
    }

    /// <summary>
    /// Export private key (for persistence - SECURE STORAGE ONLY!)
    /// </summary>
    public string ExportPrivateKey()
    {
        var rsa = GetOrCreateRsa(_keystore.ActiveKid);
        var privateKey = rsa.ExportRSAPrivateKey();
        return Convert.ToBase64String(privateKey);
    }

    private RSA GetOrCreateRsa(string kid)
    {
        if (_rsaCache.TryGetValue(kid, out var cached))
        {
            return cached;
        }

        lock (_lock)
        {
            if (_rsaCache.TryGetValue(kid, out cached))
            {
                return cached;
            }

            var record = _keystore.Keys.FirstOrDefault(x => x.Kid == kid);
            if (record == null)
            {
                throw new InvalidOperationException($"RSA keystore does not contain kid '{kid}'.");
            }

            var rsa = RSA.Create();
            var bytes = Convert.FromBase64String(record.PrivateKeyBase64);
            rsa.ImportRSAPrivateKey(bytes, out _);

            _rsaCache[kid] = rsa;
            return rsa;
        }
    }

    private void LoadOrCreateKeystore()
    {
        lock (_lock)
        {
            if (File.Exists(_keystorePath))
            {
                var json = File.ReadAllText(_keystorePath);
                _keystore = JsonSerializer.Deserialize<Keystore>(json) ?? new Keystore();
            }

            if (_keystore.Keys.Count == 0 || string.IsNullOrWhiteSpace(_keystore.ActiveKid))
            {
                var created = CreateNewKeyRecord();
                _keystore.Keys.Add(created);
                _keystore.ActiveKid = created.Kid;
            }
        }
    }

    private void PersistKeystore()
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_keystorePath)!);
            var json = JsonSerializer.Serialize(_keystore, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_keystorePath, json);
        }
    }

    private void RotateIfNeeded()
    {
        lock (_lock)
        {
            var rotationDays = GetInt("Jwt:KeyRotationDays", 30);
            if (rotationDays <= 0)
            {
                return;
            }

            var active = _keystore.Keys.FirstOrDefault(k => k.Kid == _keystore.ActiveKid);
            if (active == null)
            {
                var created = CreateNewKeyRecord();
                _keystore.Keys.Add(created);
                _keystore.ActiveKid = created.Kid;
                _activeSecurityKey = null;
                return;
            }

            if (active.CreatedAtUtc <= DateTime.UtcNow.AddDays(-rotationDays))
            {
                var created = CreateNewKeyRecord();
                _keystore.Keys.Add(created);
                _keystore.ActiveKid = created.Kid;
                _activeSecurityKey = null;
            }
        }
    }

    private void PruneIfNeeded()
    {
        lock (_lock)
        {
            var retentionDays = GetInt("Jwt:KeyRetentionDays", 90);
            if (retentionDays <= 0)
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var activeKid = _keystore.ActiveKid;

            var toRemove = _keystore.Keys
                .Where(k => k.Kid != activeKid && k.CreatedAtUtc < cutoff)
                .Select(k => k.Kid)
                .ToList();

            if (toRemove.Count == 0)
            {
                return;
            }

            _keystore.Keys.RemoveAll(k => toRemove.Contains(k.Kid));
            foreach (var kid in toRemove)
            {
                if (_rsaCache.TryRemove(kid, out var rsa))
                {
                    rsa.Dispose();
                }
            }
        }
    }

    private KeyRecord CreateNewKeyRecord()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKey();
        return new KeyRecord
        {
            Kid = Guid.NewGuid().ToString("N"),
            PrivateKeyBase64 = Convert.ToBase64String(privateKey),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private int GetInt(string key, int defaultValue)
    {
        var value = _configuration[key];
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private sealed class Keystore
    {
        public string ActiveKid { get; set; } = string.Empty;
        public List<KeyRecord> Keys { get; set; } = new();
    }

    private sealed class KeyRecord
    {
        public string Kid { get; set; } = string.Empty;
        public string PrivateKeyBase64 { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }
}
