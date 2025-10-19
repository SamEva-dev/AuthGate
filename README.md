# AuthGate — Auth service (micro-livrable #1)

## Run local
```bash
dotnet run --project src/AuthGate.Auth/AuthGate.Auth.csproj
# open http://localhost:8080/ (if you set ASPNETCORE_URLS) or the dev port shown


# 🧱 AuthGate – API d’Authentification et d’Autorisation Complète (.NET 9 + JWT + MFA + RBAC/PBAC)

> **AuthGate** est une API universelle d’authentification et d’autorisation sécurisée, basée sur **.NET 9**, **Entity Framework Core**, **JWT**, et **Google Authenticator (TOTP)**.  
> Elle combine les meilleures pratiques de **Clean Architecture**, **CQRS**, **RBAC (Role-Based Access Control)** et **PBAC (Permission-Based Access Control)**.

---

## 🔎 Sommaire
- [1️⃣ Objectif et principes](#1️⃣-objectif-et-principes)
- [2️⃣ Architecture Clean](#2️⃣-architecture-clean)
- [3️⃣ Fonctionnalités principales](#3️⃣-fonctionnalités-principales)
- [4️⃣ Sécurité et design](#4️⃣-sécurité-et-design)
- [5️⃣ Contrôleurs et endpoints](#5️⃣-contrôleurs-et-endpoints)
- [6️⃣ Gestion des rôles et permissions](#6️⃣-gestion-des-rôles-et-permissions)
- [7️⃣ Gestion des autorisations](#7️⃣-gestion-des-autorisations)
- [8️⃣ MFA (TOTP – Google Authenticator)](#8️⃣-mfa-totp--google-authenticator)
- [9️⃣ Réinitialisation de mot de passe](#9️⃣-réinitialisation-de-mot-de-passe)
- [🔁 Refresh tokens & sessions](#🔁-refresh-tokens--sessions)
- [🔍 Audit & Logs](#🔍-audit--logs)
- [🧩 Repositories et Unit of Work](#🧩-repositories-et-unit-of-work)
- [⚙️ Configuration](#⚙️-configuration)
- [🚀 Exemples d’utilisation API](#🚀-exemples-dutilisation-api)
- [📜 Licences & auteur](#📜-licences--auteur)

---

## 1️⃣ Objectif et principes

AuthGate est un **Identity Provider générique** que tu peux intégrer à n’importe quelle application :
- Application Angular, MAUI, Blazor, etc.
- Architecture microservices ou monolithique.
- Système multi-tenant.

Elle implémente :
- Une **authentification JWT complète**.
- Des **rôles et permissions dynamiques**.
- Une **vérification MFA (Google Authenticator)**.
- Une **traçabilité totale via audit log**.
- Une **API REST documentée via Swagger**.

---

## 2️⃣ Architecture Clean

### Structure des couches

### Principe Clean Architecture
- **Domain** : aucun accès à l’infrastructure.
- **Application** : logique métier, Handlers CQRS, DTOs.
- **Infrastructure** : données, services externes (email, MFA…).
- **API** : exposition HTTP, validation, sécurisation, logging.

---

## 3️⃣ Fonctionnalités principales

| Fonction | Description |
|-----------|-------------|
| 🔑 Authentification | Email + Password, JWT Access/Refresh |
| 🔒 MFA | Double authentification TOTP (Google Authenticator) |
| 🧱 RBAC | Gestion des rôles et utilisateurs |
| 🧩 PBAC | Permissions dynamiques par rôle |
| 🔁 Refresh Tokens | Rotation + Révocation + Détection de réutilisation |
| 📩 Forgot/Reset Password | Tokens à usage unique + email |
| 📜 Audit Trail | Logs SQLite + Serilog + Seq |
| 👥 Gestion Users | Liste / détail / suppression / rôles |
| ⚙️ Configurable | Via `appsettings.json` (Frontend URL, SMTP, JWT, etc.) |

---

## 4️⃣ Sécurité et design

| Mécanisme | Détails |
|------------|---------|
| **JWT** | Access (15 min) + Refresh (7 jours) |
| **Claims** | `sub`, `email`, `roles`, `permissions`, `mfa` |
| **Hashing** | PBKDF2 / BCrypt via `IPasswordHasher` |
| **MFA** | TOTP 6 chiffres via OtpNet |
| **RBAC/PBAC** | `[Authorize(Roles="Admin")]` + `[HasPermission("Code")]` |
| **Logs** | `ILogger` + `IAuditService` |
| **Audit Trail** | Traçabilité complète (action, IP, userId, timestamp) |

---

## 5️⃣ Contrôleurs et endpoints

### 🔑 AuthController
| Méthode | Route | Description |
|----------|-------|-------------|
| `POST` | `/auth/login` | Authentifie l’utilisateur |
| `POST` | `/auth/mfa/verify-login` | Vérifie le code MFA et émet le JWT |
| `POST` | `/auth/refresh` | Rafraîchit le token JWT |
| `POST` | `/auth/logout` | Révoque le refresh token courant |
| `POST` | `/auth/mfa/enable` | Génère secret + QRCode MFA |
| `POST` | `/auth/mfa/disable` | Désactive MFA |
| `POST` | `/auth/forgot-password` | Génère un token et envoie l’email |
| `POST` | `/auth/reset-password` | Réinitialise le mot de passe |

---

### 👥 UsersController
| Méthode | Route | Description |
|----------|--------|-------------|
| `GET` | `/users` | Liste des utilisateurs |
| `GET` | `/users/{id}` | Récupère un utilisateur |
| `POST` | `/users/{id}/roles` | (à venir) Assigne un rôle à un user |

> Ces routes sont protégées par la permission `CanViewUsers`.

---

### 🧩 RoleController
| Méthode | Route | Description |
|----------|-------|-------------|
| `GET` | `/roles` | Liste tous les rôles |
| `POST` | `/roles` | Crée un rôle |
| `DELETE` | `/roles/{id}` | Supprime un rôle |
| `POST` | `/roles/{roleId}/assign/{userId}` | Associe un rôle à un utilisateur |

---

### ⚙️ PermissionController
| Méthode | Route | Description |
|----------|-------|-------------|
| `GET` | `/permissions` | Liste les permissions |
| `POST` | `/permissions` | Crée une permission |
| `POST` | `/permissions/{permissionId}/assign/{roleId}` | Attribue une permission à un rôle |

---

## 6️⃣ Gestion des rôles et permissions

### Entités
```csharp
class Role { Guid Id; string Name; string Description; ICollection<UserRole> UserRoles; }
class Permission { Guid Id; string Code; string Description; ICollection<RolePermission> RolePermissions; }
class UserRole { Guid UserId; Guid RoleId; }
class RolePermission { Guid RoleId; Guid PermissionId; }

