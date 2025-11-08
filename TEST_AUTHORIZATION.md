# Guide de Test - Système d'Autorisation

## Prérequis

- AuthGate API lancée sur `http://localhost:8080`
- MailHog lancé (optionnel, pour reset password)
- PostgreSQL avec bases AuthGate et AuthGateAudit

---

## Test 1 : Login Admin et vérification permissions

### 1.1 Login

```http
POST http://localhost:8080/api/Auth/login
Content-Type: application/json

{
  "email": "admin@authgate.com",
  "password": "Admin@123"
}
```

**Réponse attendue** :
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "expiresAt": "2025-11-02T17:00:00Z",
  "requiresMfa": false
}
```

### 1.2 Vérifier les claims

```http
GET http://localhost:8080/api/TestPermissions/authenticated
Authorization: Bearer {votre-access-token}
```

**Réponse attendue** :
```json
{
  "message": "You are authenticated!",
  "userId": "...",
  "email": "admin@authgate.com",
  "roles": ["Admin"],
  "permissions": [
    "users.read",
    "users.write",
    "users.delete",
    "roles.read",
    "roles.write",
    "roles.delete",
    "permissions.read",
    "permissions.write"
  ],
  "allClaims": [...]
}
```

---

## Test 2 : Tester les endpoints avec permissions

### 2.1 Test users.read (doit fonctionner)

```http
GET http://localhost:8080/api/TestPermissions/users
Authorization: Bearer {admin-token}
```

**Résultat** : ✅ 200 OK
```json
{
  "message": "You have users.read permission!",
  "permissions": ["users.read", "users.write", ...]
}
```

### 2.2 Test users.write (doit fonctionner)

```http
POST http://localhost:8080/api/TestPermissions/users
Authorization: Bearer {admin-token}
```

**Résultat** : ✅ 200 OK

### 2.3 Test users.delete (doit fonctionner)

```http
DELETE http://localhost:8080/api/TestPermissions/users/123e4567-e89b-12d3-a456-426614174000
Authorization: Bearer {admin-token}
```

**Résultat** : ✅ 200 OK

### 2.4 Test admin-only (doit fonctionner)

```http
GET http://localhost:8080/api/TestPermissions/admin-only
Authorization: Bearer {admin-token}
```

**Résultat** : ✅ 200 OK

---

## Test 3 : Register un user normal (sans permissions)

### 3.1 Register

```http
POST http://localhost:8080/api/Register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "User@1234",
  "confirmPassword": "User@1234",
  "firstName": "John",
  "lastName": "Doe"
}
```

### 3.2 Login

```http
POST http://localhost:8080/api/Auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "User@1234"
}
```

**Token obtenu** : user sans permissions

### 3.3 Test endpoint protégé (doit échouer)

```http
GET http://localhost:8080/api/TestPermissions/users
Authorization: Bearer {user-token}
```

**Résultat** : ❌ 403 Forbidden

### 3.4 Test endpoint authentifié (doit fonctionner)

```http
GET http://localhost:8080/api/TestPermissions/authenticated
Authorization: Bearer {user-token}
```

**Résultat** : ✅ 200 OK
```json
{
  "message": "You are authenticated!",
  "userId": "...",
  "email": "user@example.com",
  "roles": [],
  "permissions": [],
  "allClaims": [...]
}
```

### 3.5 Test admin-only (doit échouer)

```http
GET http://localhost:8080/api/TestPermissions/admin-only
Authorization: Bearer {user-token}
```

**Résultat** : ❌ 403 Forbidden

---

## Test 4 : Tester sans token

### 4.1 Sans Authorization header

```http
GET http://localhost:8080/api/TestPermissions/users
```

**Résultat** : ❌ 401 Unauthorized

---

## Test 5 : Assigner des permissions à un user

### Option A : Via SQL (temporaire, en attendant endpoints CRUD)

```sql
-- Se connecter à PostgreSQL
psql -U postgres -d AuthGate

-- Trouver l'ID du user
SELECT "Id", "Email" FROM "AspNetUsers" WHERE "Email" = 'user@example.com';

-- Trouver l'ID du rôle "User"
SELECT "Id", "Name" FROM "AspNetRoles" WHERE "Name" = 'User';

-- Assigner le rôle "User" au user
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
VALUES ('user-guid-here', 'user-role-guid-here');

-- Trouver les permissions
SELECT "Id", "Code" FROM "Permissions" WHERE "Code" = 'users.read';

-- Assigner la permission au rôle User
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES ('user-role-guid-here', 'users-read-permission-guid-here');
```

### Option B : Via migrations/seeding

Modifier `AuthDbSeeder.cs` pour assigner des permissions au rôle "User" :

```csharp
var userRole = await _roleManager.FindByNameAsync("User");
if (userRole != null)
{
    var readPermissions = _context.Permissions
        .Where(p => p.Code.EndsWith(".read"))
        .ToList();
    
    foreach (var permission in readPermissions)
    {
        if (!_context.RolePermissions.Any(rp => rp.RoleId == userRole.Id && rp.PermissionId == permission.Id))
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = userRole.Id,
                PermissionId = permission.Id
            });
        }
    }
    await _context.SaveChangesAsync();
}
```

### 5.1 Re-login après assignation

```http
POST http://localhost:8080/api/Auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "User@1234"
}
```

### 5.2 Vérifier les nouvelles permissions

```http
GET http://localhost:8080/api/TestPermissions/authenticated
Authorization: Bearer {nouveau-user-token}
```

**Résultat** : permissions visibles

### 5.3 Tester users.read (doit fonctionner maintenant)

```http
GET http://localhost:8080/api/TestPermissions/users
Authorization: Bearer {nouveau-user-token}
```

**Résultat** : ✅ 200 OK

---

## Test 6 : MFA Required Policy

### 6.1 Activer MFA pour admin

```http
POST http://localhost:8080/api/Mfa/enable
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "userId": "{admin-user-id}"
}
```

### 6.2 Re-login

Le JWT contiendra `"mfa_enabled": "true"`

### 6.3 Test endpoint RequireMfa

```http
GET http://localhost:8080/api/TestPermissions/mfa-required
Authorization: Bearer {admin-mfa-token}
```

**Résultat** : ✅ 200 OK (si MFA activé et vérifié)

---

## Matrice de tests

| Endpoint | Admin (toutes perms) | User (aucune perm) | User (users.read) | Sans token |
|----------|---------------------|-------------------|-------------------|------------|
| `/authenticated` | ✅ 200 | ✅ 200 | ✅ 200 | ❌ 401 |
| `/users` (GET) | ✅ 200 | ❌ 403 | ✅ 200 | ❌ 401 |
| `/users` (POST) | ✅ 200 | ❌ 403 | ❌ 403 | ❌ 401 |
| `/users/{id}` (DELETE) | ✅ 200 | ❌ 403 | ❌ 403 | ❌ 401 |
| `/roles` (GET) | ✅ 200 | ❌ 403 | ❌ 403 | ❌ 401 |
| `/admin-only` | ✅ 200 | ❌ 403 | ❌ 403 | ❌ 401 |
| `/mfa-required` | ✅ 200* | ❌ 403 | ❌ 403 | ❌ 401 |

*Si MFA activé

---

## Codes HTTP attendus

- **200 OK** : Permission accordée, requête réussie
- **401 Unauthorized** : Token manquant ou invalide
- **403 Forbidden** : Authentifié mais permission manquante

---

## Postman Collection

Importer cette collection dans Postman :

```json
{
  "info": {
    "name": "AuthGate Authorization Tests",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "1. Login Admin",
      "request": {
        "method": "POST",
        "header": [{"key": "Content-Type", "value": "application/json"}],
        "body": {
          "mode": "raw",
          "raw": "{\"email\":\"admin@authgate.com\",\"password\":\"Admin@123\"}"
        },
        "url": "http://localhost:8080/api/Auth/login"
      }
    },
    {
      "name": "2. Get Authenticated Info",
      "request": {
        "method": "GET",
        "header": [{"key": "Authorization", "value": "Bearer {{accessToken}}"}],
        "url": "http://localhost:8080/api/TestPermissions/authenticated"
      }
    },
    {
      "name": "3. Test users.read",
      "request": {
        "method": "GET",
        "header": [{"key": "Authorization", "value": "Bearer {{accessToken}}"}],
        "url": "http://localhost:8080/api/TestPermissions/users"
      }
    }
  ]
}
```

---

## Dépannage

### Token expiré

**Erreur** : 401 après 15 minutes

**Solution** : Utiliser le refresh token

```http
POST http://localhost:8080/api/Auth/refresh
Content-Type: application/json

{
  "refreshToken": "{votre-refresh-token}"
}
```

### Permissions pas visibles après assignation

**Cause** : JWT généré avant l'assignation

**Solution** : Re-login pour obtenir un nouveau JWT

### 403 au lieu de 401

**Cause** : Token valide mais permission manquante

**Solution** : Vérifier les permissions dans `/authenticated` et les assigner si nécessaire
