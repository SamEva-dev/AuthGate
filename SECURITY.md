# Sécurité - AuthGate

## Vue d'ensemble des mesures de sécurité

AuthGate implémente plusieurs couches de sécurité pour protéger contre les attaques courantes et sécuriser les données utilisateurs.

---

## 🔐 Refresh Token Rotation avec Reuse Detection

### Principe

À chaque utilisation d'un refresh token :
1. L'ancien token est marqué comme "utilisé" (`IsUsed = true`)
2. Un nouveau token est généré
3. L'ancien token stocke l'ID du nouveau (`ReplacedByTokenId`)
4. **Si un token déjà utilisé est réutilisé → ALERTE : Tous les tokens de l'utilisateur sont révoqués**

### Implémentation

**Entité RefreshToken** :
```csharp
public class RefreshToken
{
    public bool IsUsed { get; set; }  
    public bool IsRevoked { get; set; }
    public Guid? ReplacedByTokenId { get; set; } // Pour tracer la chaîne
    public string? RevocationReason { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
```

**Handler** (`RefreshTokenCommandHandler.cs`) :
```csharp
// Détection de réutilisation
if (refreshToken.IsUsed)
{
    _logger.LogWarning("Used refresh token reused: {TokenId}, revoking all user tokens", refreshToken.Id);
    
    // Token reuse detected - SECURITY BREACH
    await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(
        refreshToken.UserId,
        "Token reuse detected",
        cancellationToken);
    
    return Result.Failure<TokenResponseDto>("Invalid refresh token");
}

// Rotation
refreshToken.IsUsed = true;
refreshToken.ReplacedByTokenId = newRefreshTokenEntity.Id;
```

### Protection contre les attaques

| Scénario | Détection | Action |
|----------|-----------|--------|
| **Token volé et utilisé** | Token marqué `IsUsed` | Génère nouveau token normalement |
| **Attaquant réutilise ancien token** | `IsUsed = true` détecté | ❌ Révocation TOUS les tokens user |
| **User légitime perd session** | L'user doit se reconnecter | Re-login requis |

### Avantages

✅ **Détection rapide** : Réutilisation = attaque potentielle  
✅ **Réponse automatique** : Révocation immédiate  
✅ **Traçabilité** : Chaîne de tokens via `ReplacedByTokenId`  
✅ **Limite les dégâts** : Attaquant perd l'accès  

---

## 🚦 Rate Limiting

### Politiques Configurées

**1. Auth Endpoints (Login/Refresh)**
```csharp
Policy: "auth"
Limite: 5 requêtes / minute
Queue: 2 requêtes
```
**Protection** : Brute force login

**2. Password Reset**
```csharp
Policy: "password-reset"
Limite: 3 requêtes / 15 minutes
Queue: 0
```
**Protection** : Email flooding, énumération

**3. Registration**
```csharp
Policy: "register"
Limite: 3 requêtes / heure
Queue: 0
```
**Protection** : Spam accounts

**4. API Générale**
```csharp
Policy: "api"
Limite: 100 requêtes / minute
Queue: 5
```
**Protection** : DoS

**5. Global Limiter**
```csharp
Partition: Par IP
Limite: 200 requêtes / minute
```
**Protection** : Attaques distribuées

### Utilisation

**Appliquer à un endpoint** :
```csharp
[HttpPost("login")]
[EnableRateLimiting("auth")]
public async Task<IActionResult> Login(...)
```

**Réponse 429 Too Many Requests** :
```json
{
  "error": "Too many requests",
  "message": "Rate limit exceeded. Please try again later.",
  "retryAfter": "60 seconds"
}
```

### Configuration

**Fichier** : `RateLimitingServiceExtensions.cs`

```csharp
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 5;
        config.Window = TimeSpan.FromMinutes(1);
    });
    
    // ... autres policies
    
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ipAddress, ...);
        });
});
```

**Middleware** : `app.UseRateLimiter();` (après `UseRouting`, avant `UseAuthentication`)

---

## 🔑 Secrets Management

### User Secrets (Développement)

**Initialisation** :
```bash
dotnet user-secrets init --project src/AuthGate.Auth/AuthGate.Auth.csproj
```

**Configurer les secrets** :
```bash
# JWT Secret
dotnet user-secrets set "Jwt:Secret" "VotreCleSuperSecreteDe32CharactersMinimum!" --project src/AuthGate.Auth

# Database Connections
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=AuthGate;Username=postgres;Password=YOUR_PASSWORD" --project src/AuthGate.Auth

dotnet user-secrets set "ConnectionStrings:AuditConnection" "Host=localhost;Port=5432;Database=AuthGateAudit;Username=postgres;Password=YOUR_PASSWORD" --project src/AuthGate.Auth
```

**Lister les secrets** :
```bash
dotnet user-secrets list --project src/AuthGate.Auth
```

### Variables d'Environnement (Production)

**Linux/Mac** :
```bash
export Jwt__Secret="VotreCléSecrète"
export ConnectionStrings__DefaultConnection="Host=..."
```

**Windows PowerShell** :
```powershell
$env:Jwt__Secret = "VotreCléSecrète"
$env:ConnectionStrings__DefaultConnection = "Host=..."
```

**Docker** :
```yaml
environment:
  - Jwt__Secret=${JWT_SECRET}
  - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
```

**Azure App Service** :
Configuration → Application settings → New application setting

**Priority** :
1. Variables d'environnement (plus haute)
2. User Secrets (dev uniquement)
3. appsettings.json (fallback)

### ⚠️ **À NE JAMAIS faire**

❌ Commit secrets dans Git  
❌ Hardcoder JWT secret  
❌ Partager connection strings  
❌ Utiliser secrets dev en prod  

✅ Utiliser User Secrets (dev)  
✅ Utiliser env vars (prod)  
✅ Stocker dans Azure Key Vault / AWS Secrets Manager  

---

## 🔒 Autres Mesures de Sécurité

### 1. Password Policy Stricte

```csharp
// IdentityServiceExtensions.cs
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = true;
options.Password.RequiredLength = 8;
```

### 2. Lockout après Échecs

```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.AllowedForNewUsers = true;
```

### 3. Protection Énumération Emails

```csharp
// Dans RequestPasswordResetCommandHandler
if (user == null)
{
    // Ne révèle PAS que l'email n'existe pas
    return Result.Success(true);
}
```

### 4. Soft Delete Users

```csharp
// DeleteUserCommandHandler
user.IsActive = false; // Pas de suppression physique
```

### 5. Révocation Sessions après Reset Password

```csharp
// ResetPasswordCommandHandler
var refreshTokens = await _context.Set<RefreshToken>()
    .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
    .ToListAsync();

foreach (var token in refreshTokens)
{
    token.IsRevoked = true;
    token.RevokedAtUtc = DateTime.UtcNow;
}
```

### 6. MFA avec Recovery Codes

- TOTP/Secret chiffré avant stockage
- Recovery codes hachés (bcrypt)
- 10 codes générés par user
- Window ±30s pour clock drift

### 7. Audit Logs Séparés

- Base PostgreSQL dédiée `AuthGateAudit`
- Logs immuables
- Tracking : login, failed login, MFA, password reset, permissions

### 8. JWT Sécurisé

```csharp
// JwtService.cs
var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
var token = new JwtSecurityToken(
    issuer: _configuration["Jwt:Issuer"],
    audience: _configuration["Jwt:Audience"],
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(15), // Courte durée
    signingCredentials: credentials
);
```

### 9. CORS Configuré

```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200"]
  }
}
```

### 10. HTTPS Enforced

```csharp
app.UseHttpsRedirection();
```

---

## 📊 Checklist Sécurité

| Mesure | Status | Implémentation |
|--------|--------|----------------|
| ✅ Refresh Token Rotation | Implémenté | `RefreshTokenCommandHandler` |
| ✅ Reuse Detection | Implémenté | Révocation automatique |
| ✅ Rate Limiting | Implémenté | 5 policies + global |
| ✅ Secrets Management | Configuré | User Secrets + env vars |
| ✅ Password Policy | Implémenté | 8+ chars, mixte, spécial |
| ✅ Lockout | Implémenté | 5 tentatives, 15 min |
| ✅ Email Enumeration Protection | Implémenté | Toujours succès |
| ✅ Soft Delete | Implémenté | IsActive flag |
| ✅ Session Revocation | Implémenté | Après reset password |
| ✅ MFA/TOTP | Implémenté | Chiffré + recovery |
| ✅ Audit Logs | Implémenté | DB séparée |
| ✅ JWT Short-lived | Implémenté | 15 minutes |
| ✅ CORS | Implémenté | Origins configurables |
| ✅ HTTPS | Implémenté | Redirection forcée |
| ✅ Permission-based Auth | Implémenté | Claims granulaires |

---

## 🛡️ Recommandations Production

### Avant Déploiement

1. ✅ Configurer secrets via env vars ou Key Vault
2. ✅ Utiliser connexions DB sécurisées (SSL)
3. ✅ Activer logs détaillés (Serilog → Seq/ELK)
4. ✅ Configurer monitoring (Prometheus/Application Insights)
5. ✅ Tester rate limiting en charge
6. ✅ Vérifier CORS origins production
7. ✅ Activer HTTPS uniquement
8. ✅ Configurer backup base audit
9. ✅ Tester recovery MFA
10. ✅ Documenter procédure incident

### Monitoring

**Alertes à configurer** :
- ❗ Taux échecs login > 10/min
- ❗ Token reuse détecté
- ❗ Rate limit 429 > 50/min
- ❗ Lockouts users > 5/heure
- ❗ Erreurs DB connexion
- ❗ JWT signature invalides

### Maintenance

**Hebdomadaire** :
- Nettoyer tokens expirés (`DeleteExpiredTokensAsync`)
- Vérifier audit logs anormaux
- Review lockouts répétés

**Mensuel** :
- Rotation JWT secret (avec période transition)
- Audit permissions utilisateurs
- Review policies rate limiting

---

## 📚 Ressources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [NIST Password Guidelines](https://pages.nist.gov/800-63-3/sp800-63b.html)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [ASP.NET Core Security](https://docs.microsoft.com/aspnet/core/security)

---

## ✅ Conclusion

AuthGate implémente **toutes les meilleures pratiques de sécurité** pour une API d'authentification moderne :

- 🔐 Tokens sécurisés avec rotation
- 🚦 Protection rate limiting
- 🔑 Secrets management proper
- 🛡️ Défense en profondeur (multiple couches)
- 📊 Audit complet
- ⚡ Réponse rapide aux incidents

**Prêt pour production !**
