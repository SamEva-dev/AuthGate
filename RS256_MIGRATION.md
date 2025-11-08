# Migration AuthGate: HS256 → RS256

## ✅ Ce qui a été fait

### 1. Service RSA Key Management
**Fichier**: `AuthGate.Auth.Infrastructure/Services/RsaKeyService.cs`

- Génération automatique paire RSA 2048 bits au démarrage
- Key ID unique (GUID) pour identifier la clé dans JWKS
- Export/Import PEM pour persistence (si nécessaire)
- Méthodes pour obtenir clé de signature et paramètres publics

### 2. JwtService migré en RS256
**Fichier**: `AuthGate.Auth.Infrastructure/Services/JwtService.cs`

- Utilise `RsaSecurityKey` au lieu de `SymmetricSecurityKey`
- Algorithme: `SecurityAlgorithms.RsaSha256` (au lieu de HmacSha256)
- Injection de `RsaKeyService` pour obtenir la clé de signature

### 3. Endpoint JWKS
**Fichier**: `AuthGate.Auth/Controllers/JwksController.cs`

- Route: `GET /.well-known/jwks.json`
- Expose la clé publique RSA au format JWKS
- Format standard: `{ "keys": [{ "kty": "RSA", "use": "sig", "kid": "...", "alg": "RS256", "n": "...", "e": "..." }] }`
- Accessible sans authentification (AllowAnonymous)

### 4. Enregistrement DI
**Fichier**: `AuthGate.Auth.Infrastructure/DependencyInjection.cs`

- `RsaKeyService` enregistré en **Singleton** (clé persiste durant la vie de l'application)
- Injection dans `JwtService`

---

## 🔐 Avantages RS256 vs HS256

| Aspect | HS256 (avant) | RS256 (maintenant) |
|--------|---------------|-------------------|
| **Type** | Symétrique (secret partagé) | Asymétrique (clé publique/privée) |
| **Secret** | Partagé entre AuthGate et LocaGuest | Clé privée AuthGate ONLY, publique via JWKS |
| **Sécurité** | Si secret compromis → tout compromis | Clé privée jamais exposée |
| **Scalabilité** | Tous les services doivent connaître le secret | Services valident via clé publique JWKS |
| **Rotation** | Complexe (redéployer tous les services) | Simple (nouveau kid, ancienne clé reste active) |

---

## 🧪 Tester JWKS

### 1. Démarrer AuthGate
```bash
dotnet run --project src/AuthGate.Auth
```

### 2. Obtenir le JWKS
```bash
curl http://localhost:8080/.well-known/jwks.json
```

**Réponse attendue**:
```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "kid": "abc123...",
      "alg": "RS256",
      "n": "base64url_modulus...",
      "e": "AQAB"
    }
  ]
}
```

### 3. Login et vérifier JWT
```bash
# Login
curl -X POST http://localhost:8080/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authgate.com","password":"Admin@123"}'

# Copier le token, décoder sur jwt.io
# Header devrait montrer: "alg": "RS256", "kid": "..."
```

---

## 🔄 Configuration LocaGuest.API (prochaine étape)

```csharp
// Startup.cs ou Program.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080"; // URL AuthGate
        options.RequireHttpsMetadata = false; // dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "AuthGate",
            ValidateAudience = true,
            ValidAudience = "AuthGate",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // Clé obtenue automatiquement via /.well-known/jwks.json
        };
    });
```

---

## 🔐 Rotation des clés (future)

Pour supporter plusieurs clés actives simultanément (rotation sans downtime):

1. Générer nouvelle paire RSA avec nouveau `kid`
2. Ajouter au tableau `keys` du JWKS
3. Signer nouveaux tokens avec nouvelle clé
4. Ancienne clé reste dans JWKS pour valider anciens tokens
5. Après expiration des anciens tokens (ex: 1h), retirer ancienne clé du JWKS

---

## 📦 Persistence des clés (production)

**Dev**: Clé générée au démarrage, perdue au redémarrage (OK pour dev).

**Prod** (recommandations):
- **Azure Key Vault** / **AWS KMS**: stocker clé privée chiffrée
- **Docker Secrets**: monter clé privée PEM
- **Variables d'environnement**: base64 de la clé privée RSA

Exemple import clé:
```csharp
var base64Key = Environment.GetEnvironmentVariable("RSA_PRIVATE_KEY");
if (!string.IsNullOrEmpty(base64Key))
{
    rsaKeyService.ImportKey(base64Key);
}
```

---

## ✅ Checklist

- [x] RsaKeyService créé (génération clé 2048 bits)
- [x] JwtService migré RS256
- [x] Endpoint JWKS exposé (/.well-known/jwks.json)
- [x] DI configuré (Singleton RsaKeyService)
- [ ] Tester JWKS endpoint
- [ ] Tester login avec JWT RS256
- [ ] Configurer LocaGuest.API validation RS256
- [ ] Documenter rotation des clés

---

## 🎯 Prochaine étape

Configurer **LocaGuest.API** pour valider les JWT RS256 via le JWKS d'AuthGate.
