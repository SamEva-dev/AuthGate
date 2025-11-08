# Authorization System - AuthGate

## Overview

AuthGate implémente un système d'autorisation basé sur les **permissions** (permission-based access control) avec support des **rôles** pour une gestion simplifiée.

## Architecture

### 1. Claims dans JWT

Lors du login, le JWT contient automatiquement :
- **Rôles** : `ClaimTypes.Role` (ex: "Admin", "User", "Manager")
- **Permissions** : `"permission"` (ex: "users.read", "users.write")

```csharp
// Exemple de claims dans le JWT
{
  "sub": "user-guid",
  "email": "admin@authgate.com",
  "role": ["Admin"],
  "permission": ["users.read", "users.write", "users.delete", "roles.read", ...],
  "mfa_enabled": "false"
}
```

### 2. Composants du système

#### **PermissionRequirement**
`AuthGate.Auth.Authorization.PermissionRequirement`
- Représente une exigence de permission pour accéder à une ressource

#### **PermissionAuthorizationHandler**
`AuthGate.Auth.Authorization.PermissionAuthorizationHandler`
- Vérifie si l'utilisateur possède la permission requise dans ses claims

#### **PermissionPolicyProvider**
`AuthGate.Auth.Authorization.PermissionPolicyProvider`
- Fournisseur de policies dynamiques basé sur les permissions
- Génère automatiquement des policies au format `"Permission:{code}"`

#### **HasPermissionAttribute**
`AuthGate.Auth.Authorization.HasPermissionAttribute`
- Attribut simplifié pour protéger les endpoints
- Usage : `[HasPermission("users.read")]`

---

## Utilisation

### 1. Protéger un endpoint avec une permission

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiert authentification
public class UsersController : ControllerBase
{
    // Requiert la permission "users.read"
    [HttpGet]
    [HasPermission("users.read")]
    public async Task<IActionResult> GetUsers()
    {
        // Seuls les utilisateurs avec "users.read" peuvent accéder
        return Ok(users);
    }

    // Requiert la permission "users.write"
    [HttpPost]
    [HasPermission("users.write")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // Seuls les utilisateurs avec "users.write" peuvent accéder
        return Ok(newUser);
    }

    // Requiert la permission "users.delete"
    [HttpDelete("{id}")]
    [HasPermission("users.delete")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        // Seuls les utilisateurs avec "users.delete" peuvent accéder
        return NoContent();
    }
}
```

### 2. Protéger avec un rôle

```csharp
[HttpGet("admin-only")]
[Authorize(Policy = "AdminOnly")]
public IActionResult AdminEndpoint()
{
    // Seuls les utilisateurs avec le rôle "Admin" peuvent accéder
    return Ok();
}
```

### 3. Combiner plusieurs permissions

```csharp
[HttpPut("{id}")]
[HasPermission("users.read")]
[HasPermission("users.write")]
public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
{
    // Requiert BOTH permissions
    return Ok();
}
```

### 4. Vérifier les permissions dans le code

```csharp
public class UserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public bool CanDeleteUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.HasClaim("permission", "users.delete") ?? false;
    }

    public IEnumerable<string> GetUserPermissions()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindAll("permission").Select(c => c.Value) ?? Enumerable.Empty<string>();
    }
}
```

---

## Permissions par défaut

### Users
- `users.read` - Lire les utilisateurs
- `users.write` - Créer/modifier les utilisateurs
- `users.delete` - Supprimer les utilisateurs

### Roles
- `roles.read` - Lire les rôles
- `roles.write` - Créer/modifier les rôles
- `roles.delete` - Supprimer les rôles

### Permissions
- `permissions.read` - Lire les permissions
- `permissions.write` - Assigner/retirer des permissions

---

## Policies prédéfinies

### AdminOnly
```csharp
[Authorize(Policy = "AdminOnly")]
```
Requiert le rôle "Admin"

### RequireMfa
```csharp
[Authorize(Policy = "RequireMfa")]
```
Requiert que l'utilisateur ait MFA activé

---

## Tester l'autorisation

### 1. Login en tant qu'admin

```bash
POST http://localhost:8080/api/Auth/login
Content-Type: application/json

{
  "email": "admin@authgate.com",
  "password": "Admin@123"
}
```

**Réponse** :
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiresAt": "2025-11-02T17:00:00Z"
}
```

### 2. Utiliser le token

```bash
GET http://localhost:8080/api/TestPermissions/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Réponse 200** (admin a `users.read`) :
```json
{
  "message": "You have users.read permission!",
  "permissions": ["users.read", "users.write", "users.delete", ...]
}
```

### 3. Tester sans permission

```bash
# Login avec un user normal sans permissions
POST http://localhost:8080/api/Auth/login
{
  "email": "user@example.com",
  "password": "User@123"
}

# Tenter d'accéder à un endpoint protégé
GET http://localhost:8080/api/TestPermissions/users
Authorization: Bearer {user-token}
```

**Réponse 403 Forbidden** :
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

---

## Endpoints de test

Le controller `TestPermissionsController` fournit des endpoints pour tester :

| Endpoint | Permission | Description |
|----------|-----------|-------------|
| `GET /api/TestPermissions/users` | `users.read` | Test lecture users |
| `POST /api/TestPermissions/users` | `users.write` | Test création users |
| `DELETE /api/TestPermissions/users/{id}` | `users.delete` | Test suppression users |
| `GET /api/TestPermissions/roles` | `roles.read` | Test lecture roles |
| `GET /api/TestPermissions/admin-only` | Role: Admin | Test rôle Admin |
| `GET /api/TestPermissions/authenticated` | Authentifié | Voir tous les claims |
| `GET /api/TestPermissions/mfa-required` | MFA enabled | Test MFA requis |

---

## Ajouter de nouvelles permissions

### 1. Ajouter la permission en base

```csharp
// Dans AuthDbSeeder.cs
var newPermission = new Permission
{
    Id = Guid.NewGuid(),
    Code = "reports.generate",
    NormalizedCode = "REPORTS.GENERATE",
    DisplayName = "Generate Reports",
    Description = "Can generate financial reports",
    Category = "Reports",
    CreatedAtUtc = DateTime.UtcNow
};
context.Permissions.Add(newPermission);
```

### 2. Assigner à un rôle

```csharp
// Via admin endpoints (à créer) ou directement en base
var rolePermission = new RolePermission
{
    RoleId = adminRoleId,
    PermissionId = newPermission.Id
};
context.RolePermissions.Add(rolePermission);
```

### 3. Utiliser dans un endpoint

```csharp
[HttpGet("financial-report")]
[HasPermission("reports.generate")]
public async Task<IActionResult> GenerateFinancialReport()
{
    return Ok(report);
}
```

---

## Bonnes pratiques

### 1. Nommer les permissions

Format recommandé : `{resource}.{action}`

Exemples :
- ✅ `users.read`, `users.write`, `users.delete`
- ✅ `orders.approve`, `orders.cancel`
- ✅ `reports.generate`, `reports.export`
- ❌ `canReadUsers`, `UserRead`

### 2. Granularité

- Préférer plusieurs permissions fines plutôt qu'une seule globale
- ✅ `users.read` + `users.write` + `users.delete`
- ❌ `users.manage` (trop large)

### 3. Catégories

Grouper les permissions par ressource pour faciliter la gestion :
- Users : `users.*`
- Roles : `roles.*`
- Reports : `reports.*`
- Settings : `settings.*`

### 4. Sécurité

```csharp
// ❌ MAUVAIS - Pas de protection
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(Guid id) { }

// ✅ BON - Protection par permission
[HttpDelete("{id}")]
[HasPermission("users.delete")]
public async Task<IActionResult> DeleteUser(Guid id) { }

// ✅ MIEUX - Protection + validation propriétaire
[HttpDelete("{id}")]
[HasPermission("users.delete")]
public async Task<IActionResult> DeleteUser(Guid id)
{
    var currentUserId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    // Vérifier que l'user peut bien supprimer cet ID
    if (!await _userService.CanDeleteUser(id, currentUserId))
        return Forbid();
    
    await _userService.DeleteAsync(id);
    return NoContent();
}
```

---

## Troubleshooting

### Erreur 401 Unauthorized

**Cause** : Token JWT manquant ou invalide

**Solution** :
```bash
# Vérifier que le header Authorization est présent
Authorization: Bearer {votre-token-jwt}
```

### Erreur 403 Forbidden

**Cause** : Utilisateur authentifié mais sans la permission requise

**Solutions** :
1. Vérifier les permissions de l'utilisateur :
   ```bash
   GET /api/TestPermissions/authenticated
   ```
2. Assigner la permission au rôle de l'utilisateur en base
3. Re-login pour obtenir un nouveau JWT avec les nouvelles permissions

### Permission non prise en compte

**Cause** : JWT généré avant l'ajout de la permission

**Solution** : L'utilisateur doit se déconnecter et se reconnecter pour obtenir un nouveau JWT avec les permissions à jour

---

## Configuration

### Startup.cs

```csharp
// Configurer l'autorisation
services.AddPermissionAuthorization();

// Utiliser dans le pipeline
app.UseAuthentication();
app.UseAuthorization(); // IMPORTANT : après UseAuthentication
```

### appsettings.json

```json
{
  "Jwt": {
    "Secret": "YourSecretKey...",
    "Issuer": "AuthGate",
    "Audience": "AuthGate",
    "AccessTokenExpirationMinutes": 15
  }
}
```

---

## Prochaines évolutions

- [ ] Endpoints CRUD pour gérer roles/permissions via API
- [ ] Permission inheritance (hierarchie de permissions)
- [ ] Permission scopes (permissions temporaires)
- [ ] Audit logging des accès denied
- [ ] Dashboard permissions par utilisateur
