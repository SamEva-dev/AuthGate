# Rapport de Vérification - AuthGate API Sécurité

**Date:** 15 novembre 2025  
**Statut:** ⚠️ PARTIELLEMENT CONFORME - Correctifs requis

---

## ✅ Points Conformes

### 1. ✅ Séparation Complète AuthGate/LocaGuest

#### Aucune logique métier LocaGuest
**Entités AuthGate (Domain):**
- `User` - Authentification uniquement
- `RefreshToken` - Gestion tokens
- `MfaSecret` - Authentification MFA
- `RecoveryCode` - Codes de récupération
- `PasswordResetToken` - Réinitialisation password
- `Role` - Rôles d'autorisation
- `Permission` - Permissions granulaires
- `RolePermission` - Mapping rôles/permissions
- `AuditLog` - Logs d'audit

**Résultat:** ✅ **AUCUNE** entité ou logique métier de LocaGuest

---

#### Aucune table Stripe
**DbContext:** `AuthDbContext`

```csharp
public DbSet<Permission> Permissions => Set<Permission>();
public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
public DbSet<MfaSecret> MfaSecrets => Set<MfaSecret>();
public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();
public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
```

**Tables Identity:**
- Users
- Roles
- UserClaims
- UserRoles
- UserLogins
- RoleClaims
- UserTokens

**Résultat:** ✅ **AUCUNE** référence à Stripe, Property, Contract, Subscription, Payment, etc.

---

#### Aucune dépendance LocaGuest
**Recherche dans le code:**
```bash
Recherche: "LocaGuest|Property|Contract|Subscription|Stripe"
Résultat: ❌ Aucune occurrence (sauf dans .csproj pour PropertyGroup XML)
```

**Résultat:** ✅ **AUCUNE** dépendance vers LocaGuest

---

### 2. ⚠️ Claims JWT - INCOMPLET

#### Claims Actuels

**Code `JwtService.GenerateAccessToken()`:**

```csharp
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, userId.ToString()),      // ✅ sub
    new(JwtRegisteredClaimNames.Email, email),                // ✅ email
    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new("mfa_enabled", mfaEnabled.ToString().ToLower())
};

foreach (var role in roles)
{
    claims.Add(new Claim(ClaimTypes.Role, role));             // ✅ roles
}

foreach (var permission in permissions)
{
    claims.Add(new Claim("permission", permission));
}
```

**Claims générés:**
- ✅ `sub` (userId)
- ✅ `email`
- ✅ `roles` (via ClaimTypes.Role)
- ✅ `jti` (JWT ID)
- ⚠️ `mfa_enabled`
- ⚠️ `permission` (multiples)

**Claims MANQUANTS:**
- ❌ **`tenant_id`** - **CRITIQUE** pour LocaGuest !
- ❌ `preferred_language` (optionnel mais utile)

---

### 3. ✅ Sécurité des Refresh Tokens

#### Entité RefreshToken

```csharp
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public required string Token { get; set; }
    public required string JwtId { get; set; }
    
    public bool IsUsed { get; set; }                    // ✅ Protection réutilisation
    public bool IsRevoked { get; set; }                 // ✅ Révocation
    public DateTime ExpiresAtUtc { get; set; }          // ✅ Durée limitée
    
    public string? RevocationReason { get; set; }       // ✅ Traçabilité
    public DateTime? RevokedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }            // ✅ Sécurité IP
    public Guid? ReplacedByTokenId { get; set; }        // ✅ Rotation
    
    public virtual User User { get; set; } = null!;
}
```

**Génération du token:**

```csharp
public string GenerateRefreshToken()
{
    var randomNumber = new byte[64];                    // ✅ 64 bytes = 512 bits
    using var rng = RandomNumberGenerator.Create();     // ✅ Cryptographiquement sécurisé
    rng.GetBytes(randomNumber);
    return Convert.ToBase64String(randomNumber);
}
```

**Stockage dans LoginCommandHandler:**

```csharp
var refreshTokenEntity = new RefreshToken
{
    Id = Guid.NewGuid(),
    UserId = user.Id,
    Token = refreshToken,
    JwtId = jwtId,
    ExpiresAtUtc = DateTime.UtcNow.AddDays(7),         // ✅ 7 jours max
    CreatedAtUtc = DateTime.UtcNow
};
```

**Vérifications de sécurité:**
- ✅ **Rotation automatique** via `ReplacedByTokenId`
- ✅ **Révocation** via `IsRevoked` + raison
- ✅ **Protection réutilisation** via `IsUsed`
- ✅ **Durée limitée** (7 jours)
- ✅ **Cryptographiquement sécurisé** (RandomNumberGenerator)
- ⚠️ **HttpOnly:** À vérifier dans les cookies (non visible dans ce code)

**Résultat:** ✅ **CONFORME** - Refresh tokens très bien sécurisés

---

### 4. ✅ Hashing des Passwords

**Implémentation:** `PasswordHasher.cs`

```csharp
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string hash, string password)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrEmpty(password))
        {
            return false;
        }
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Invalid hash format or algorithm mismatch
            return false;
        }
    }
}
```

**Vérifications:**
- ✅ **BCrypt** avec work factor 12 (2^12 = 4096 itérations)
- ✅ **Salt automatique** (intégré dans BCrypt)
- ✅ **Protection timing attacks** (BCrypt vérifie en temps constant)
- ✅ **Gestion erreurs** sans révéler d'informations

**Résultat:** ✅ **CONFORME** - BCrypt est recommandé (OWASP)

---

### 5. ✅ Séparation des Rôles

**Rôles prédéfinis (AuthDbSeeder.cs):**

```csharp
private async Task SeedRolesAsync()
{
    var roles = new[]
    {
        new { Name = "Admin", Description = "Administrator with full access", IsSystemRole = true },
        new { Name = "User", Description = "Standard user", IsSystemRole = false },
        new { Name = "Manager", Description = "Manager with elevated permissions", IsSystemRole = false }
    };
    // ...
}
```

**Vérifications:**
- ✅ **Admin** - Administrateur complet
- ✅ **User** - Utilisateur standard
- ✅ **Manager** - Gestionnaire (permissions élevées)
- ⚠️ **Pas de rôle "Owner"** mentionné dans les requirements

**Système de permissions:**
- ✅ Permissions granulaires via `Permission` entity
- ✅ Mapping flexible via `RolePermission`
- ✅ Rôles système protégés (non supprimables)

**Résultat:** ✅ **CONFORME** - Mais ajouter "Owner" si nécessaire

---

### 6. ✅ Sécurité du Login

**Implémentation:** `LoginCommandHandler.cs`

#### Protection contre User Enumeration

```csharp
// Ligne 47-53
var user = await _userManager.FindByEmailAsync(request.Email);

if (user == null)
{
    _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
    return Result.Failure<LoginResponseDto>("Invalid email or password");  // ✅
}
```

```csharp
// Ligne 74-78
if (!result.Succeeded)
{
    _logger.LogWarning("Invalid password for user: {UserId}", user.Id);
    return Result.Failure<LoginResponseDto>("Invalid email or password");  // ✅
}
```

**Résultat:** ✅ **Message identique** pour user inexistant ET password invalide

---

#### Protection contre Brute Force

```csharp
// Ligne 65
var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

// Ligne 67-72
if (result.IsLockedOut)
{
    _logger.LogWarning("Login attempt for locked user: {UserId}", user.Id);
    var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
    return Result.Failure<LoginResponseDto>($"Account is locked until {lockoutEnd?.UtcDateTime:g} UTC");
}
```

**Résultat:** ✅ **Lockout automatique** après échecs répétés

---

#### Vérifications Supplémentaires

```csharp
// Ligne 58-62 - Compte désactivé
if (!user.IsActive)
{
    _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
    return Result.Failure<LoginResponseDto>("Account is inactive");
}

// Ligne 81-92 - MFA obligatoire
if (user.MfaEnabled)
{
    var mfaToken = _jwtService.GenerateRefreshToken();
    var response = new LoginResponseDto
    {
        RequiresMfa = true,
        MfaToken = mfaToken
    };
    // ...
}
```

**Résultat:** ✅ **Multiples couches de protection**

---

## ❌ Points Non Conformes - CORRECTIONS REQUISES

### 1. ❌ CRITIQUE: `tenant_id` manquant dans JWT

**Problème:**
Le claim `tenant_id` est **ESSENTIEL** pour l'architecture multi-tenant de LocaGuest, mais il n'est **PAS** généré par AuthGate.

**Impact:**
- LocaGuest ne peut pas isoler les données par tenant
- L'architecture multi-tenant ne fonctionne pas
- Risque de fuite de données entre tenants

**Solution Requise:**

#### Étape 1: Ajouter TenantId dans User.cs

```csharp
// AuthGate.Auth.Domain/Entities/User.cs
public class User : IdentityUser<Guid>, IAuditableEntity
{
    // ... propriétés existantes ...
    
    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant isolation
    /// </summary>
    public string? TenantId { get; set; }
    
    // ... reste du code ...
}
```

#### Étape 2: Modifier JwtService.GenerateAccessToken()

```csharp
// AuthGate.Auth.Infrastructure/Services/JwtService.cs
public string GenerateAccessToken(
    Guid userId, 
    string email, 
    IEnumerable<string> roles, 
    IEnumerable<string> permissions, 
    bool mfaEnabled,
    string? tenantId = null)  // ← Ajouter paramètre
{
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new(JwtRegisteredClaimNames.Email, email),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new("mfa_enabled", mfaEnabled.ToString().ToLower())
    };
    
    // ← AJOUTER CECI
    if (!string.IsNullOrEmpty(tenantId))
    {
        claims.Add(new Claim("tenant_id", tenantId));
    }
    
    foreach (var role in roles)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }
    // ... reste du code ...
}
```

#### Étape 3: Mettre à jour LoginCommandHandler

```csharp
// AuthGate.Auth.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs
// Ligne 102
var accessToken = _jwtService.GenerateAccessToken(
    user.Id, 
    user.Email!, 
    roles, 
    permissions, 
    user.MfaEnabled,
    user.TenantId);  // ← Ajouter tenantId
```

#### Étape 4: Créer une migration

```bash
cd AuthGate/src/AuthGate.Auth.Infrastructure
dotnet ef migrations add AddTenantIdToUser -s ../../src/AuthGate.Auth
```

---

### 2. ⚠️ OPTIONNEL: `preferred_language` manquant

**Problème:**
Le claim `preferred_language` est optionnel mais utile pour l'UX.

**Solution (si souhaité):**

```csharp
// User.cs
public string? PreferredLanguage { get; set; } = "en";

// JwtService.cs
if (!string.IsNullOrEmpty(preferredLanguage))
{
    claims.Add(new Claim("preferred_language", preferredLanguage));
}
```

---

### 3. ⚠️ Rôle "Owner" manquant

**Problème:**
Le requirement mentionne User/Admin/Owner, mais seul "Manager" existe.

**Solution:**

```csharp
// AuthDbSeeder.cs
private async Task SeedRolesAsync()
{
    var roles = new[]
    {
        new { Name = "Admin", Description = "Administrator with full access", IsSystemRole = true },
        new { Name = "User", Description = "Standard user", IsSystemRole = false },
        new { Name = "Owner", Description = "Property owner with management rights", IsSystemRole = false },
        new { Name = "Manager", Description = "Manager with elevated permissions", IsSystemRole = false }
    };
    // ...
}
```

---

## 📋 Checklist AuthGate - Sécurité

### Séparation AuthGate/LocaGuest
- [x] Aucune logique métier LocaGuest
- [x] Aucune table Stripe
- [x] Aucune dépendance LocaGuest
- [x] Base de données indépendante

### Claims JWT
- [x] `sub` (userId)
- [x] `email`
- [x] `roles`
- [x] `jti` (JWT ID)
- [ ] **`tenant_id`** ❌ **MANQUANT - CRITIQUE**
- [ ] `preferred_language` ⚠️ Optionnel

### Sécurité Refresh Tokens
- [x] Rotation automatique
- [x] Révocation supportée
- [x] Protection réutilisation
- [x] Durée limitée (7 jours)
- [x] Cryptographiquement sécurisé
- [?] HttpOnly cookies (non vérifié dans ce code)

### Hashing Passwords
- [x] BCrypt utilisé
- [x] Work factor 12 (sécurisé)
- [x] Salt automatique
- [x] Protection timing attacks

### Rôles
- [x] Admin
- [x] User
- [x] Manager
- [ ] Owner ⚠️ Manquant (selon requirements)

### Sécurité Login
- [x] Pas de user enumeration
- [x] Lockout automatique
- [x] Gestion MFA
- [x] Compte inactif détecté
- [x] Messages d'erreur génériques

---

## 🚨 Actions Immédiates Requises

### PRIORITÉ CRITIQUE

1. **Ajouter `TenantId` dans User entity**
2. **Ajouter claim `tenant_id` dans JWT**
3. **Créer migration pour TenantId**
4. **Tester l'intégration avec LocaGuest**

### PRIORITÉ MOYENNE

5. **Ajouter rôle "Owner" (si besoin métier)**
6. **Ajouter `preferred_language` (optionnel)**
7. **Vérifier HttpOnly cookies pour refresh tokens**

---

## ✅ Conclusion

**Statut Actuel:** ⚠️ **PARTIELLEMENT CONFORME**

**Points Forts:**
- ✅ Séparation parfaite AuthGate/LocaGuest
- ✅ Aucune table Stripe ni logique métier LocaGuest
- ✅ Sécurité passwords excellente (BCrypt work factor 12)
- ✅ Refresh tokens très bien sécurisés
- ✅ Protection login complète (lockout, MFA, pas d'user enumeration)
- ✅ Rôles et permissions granulaires

**Points Bloquants:**
- ❌ **`tenant_id` manquant dans JWT** - **BLOQUE l'architecture multi-tenant**

**Recommandation:**
Corriger immédiatement l'absence de `tenant_id` avant tout déploiement. Sans ce claim, l'architecture multi-tenant de LocaGuest **NE PEUT PAS FONCTIONNER**.

**Prochaines Étapes:**
1. Appliquer les corrections ci-dessus
2. Créer et appliquer la migration
3. Tester l'intégration AuthGate → LocaGuest
4. Vérifier l'isolation multi-tenant en production
