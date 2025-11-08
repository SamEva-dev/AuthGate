# AuthGate API - Endpoints Documentation

## Base URL
```
http://localhost:8080/api
```

---

## 🔐 Authentication Endpoints

### Register
```http
POST /api/Register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePass@123",
  "confirmPassword": "SecurePass@123",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890"
}
```

### Login
```http
POST /api/Auth/login
Content-Type: application/json

{
  "email": "admin@authgate.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "base64-token",
  "expiresAt": "2025-11-02T17:00:00Z",
  "requiresMfa": false
}
```

### Refresh Token
```http
POST /api/Auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

### Logout
```http
POST /api/Auth/logout
Content-Type: application/json

{
  "refreshToken": "your-refresh-token"
}
```

---

## 🔑 Password Reset

### Request Password Reset
```http
POST /api/PasswordReset/request
Content-Type: application/json

{
  "email": "user@example.com"
}
```

### Reset Password
```http
POST /api/PasswordReset/reset
Content-Type: application/json

{
  "email": "user@example.com",
  "token": "reset-token-from-email",
  "newPassword": "NewSecure@123",
  "confirmPassword": "NewSecure@123"
}
```

---

## 🛡️ MFA Endpoints

### Enable MFA
```http
POST /api/Mfa/enable
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "user-guid"
}
```

**Response:**
```json
{
  "secretKey": "BASE32ENCODEDSECRET",
  "qrCodeDataUri": "otpauth://totp/...",
  "manualEntryKey": "ABCD EFGH IJKL...",
  "recoveryCodes": ["CODE1", "CODE2", ...]
}
```

### Verify MFA
```http
POST /api/Mfa/verify
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "user-guid",
  "secret": "secret-from-enable-step",
  "code": "123456"
}
```

### Disable MFA
```http
POST /api/Mfa/disable
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "user-guid",
  "password": "current-password"
}
```

---

## 👥 Users Management

**Required Permission:** `users.read`, `users.write`, `users.delete`

### Get All Users (Paginated)
```http
GET /api/Users?page=1&pageSize=10&search=john&isActive=true
Authorization: Bearer {token}
```

**Response:**
```json
{
  "items": [
    {
      "id": "guid",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "phoneNumber": "+1234567890",
      "isActive": true,
      "mfaEnabled": false,
      "emailConfirmed": true,
      "createdAtUtc": "2025-11-02T10:00:00Z",
      "lastLoginAtUtc": "2025-11-02T15:00:00Z",
      "roles": ["User"]
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### Get User By ID
```http
GET /api/Users/{id}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "id": "guid",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890",
  "isActive": true,
  "mfaEnabled": false,
  "emailConfirmed": true,
  "failedLoginAttempts": 0,
  "isLockedOut": false,
  "lockoutEndUtc": null,
  "lastLoginAtUtc": "2025-11-02T15:00:00Z",
  "createdAtUtc": "2025-11-02T10:00:00Z",
  "updatedAtUtc": null,
  "roles": ["User", "Manager"],
  "permissions": ["users.read", "roles.read"]
}
```

### Update User
```http
PUT /api/Users/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "userId": "same-as-path-id",
  "firstName": "Jane",
  "lastName": "Smith",
  "phoneNumber": "+9876543210",
  "isActive": true
}
```

### Delete User (Soft Delete)
```http
DELETE /api/Users/{id}
Authorization: Bearer {token}
```

**Response:** `204 No Content`

---

## 🎭 Roles Management

**Required Permission:** `roles.read`, `permissions.write`

### Get All Roles
```http
GET /api/Roles
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "id": "guid",
    "name": "Admin",
    "description": "Administrator with full access",
    "isSystemRole": true,
    "createdAtUtc": "2025-11-01T00:00:00Z",
    "userCount": 5,
    "permissionCount": 8
  },
  {
    "id": "guid",
    "name": "User",
    "description": "Standard user",
    "isSystemRole": false,
    "createdAtUtc": "2025-11-01T00:00:00Z",
    "userCount": 120,
    "permissionCount": 2
  }
]
```

### Assign Permission to Role
```http
POST /api/Roles/{roleId}/permissions/{permissionId}
Authorization: Bearer {token}
```

**Response:**
```json
{
  "message": "Permission assigned"
}
```

### Remove Permission from Role
```http
DELETE /api/Roles/{roleId}/permissions/{permissionId}
Authorization: Bearer {token}
```

**Response:** `204 No Content`

---

## 🔓 Permissions Management

**Required Permission:** `permissions.read`

### Get All Permissions
```http
GET /api/Permissions
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "id": "guid",
    "code": "users.read",
    "displayName": "Read Users",
    "description": "Can view user information",
    "category": "Users",
    "isActive": true
  },
  {
    "id": "guid",
    "code": "users.write",
    "displayName": "Write Users",
    "description": "Can create and update users",
    "category": "Users",
    "isActive": true
  }
]
```

---

## 🧪 Test Endpoints (Development Only)

### Get Authenticated User Info
```http
GET /api/TestPermissions/authenticated
Authorization: Bearer {token}
```

### Test users.read Permission
```http
GET /api/TestPermissions/users
Authorization: Bearer {token}
```

### Test users.write Permission
```http
POST /api/TestPermissions/users
Authorization: Bearer {token}
```

### Test users.delete Permission
```http
DELETE /api/TestPermissions/users/{id}
Authorization: Bearer {token}
```

### Test Admin Role
```http
GET /api/TestPermissions/admin-only
Authorization: Bearer {token}
```

### Test MFA Required
```http
GET /api/TestPermissions/mfa-required
Authorization: Bearer {token}
```

---

## 📊 Endpoint Summary

| Category | Endpoint | Method | Permission Required |
|----------|----------|--------|---------------------|
| **Auth** | `/Register` | POST | None (Public) |
| | `/Auth/login` | POST | None (Public) |
| | `/Auth/refresh` | POST | None (Public) |
| | `/Auth/logout` | POST | None (Public) |
| **Password** | `/PasswordReset/request` | POST | None (Public) |
| | `/PasswordReset/reset` | POST | None (Public) |
| **MFA** | `/Mfa/enable` | POST | Authenticated |
| | `/Mfa/verify` | POST | Authenticated |
| | `/Mfa/disable` | POST | Authenticated |
| **Users** | `/Users` | GET | `users.read` |
| | `/Users/{id}` | GET | `users.read` |
| | `/Users/{id}` | PUT | `users.write` |
| | `/Users/{id}` | DELETE | `users.delete` |
| **Roles** | `/Roles` | GET | `roles.read` |
| | `/Roles/{roleId}/permissions/{permissionId}` | POST | `permissions.write` |
| | `/Roles/{roleId}/permissions/{permissionId}` | DELETE | `permissions.write` |
| **Permissions** | `/Permissions` | GET | `permissions.read` |
| **Test** | `/TestPermissions/*` | Various | Various |

---

## 🔒 Default Permissions

- `users.read` - Read users
- `users.write` - Create/update users
- `users.delete` - Delete users
- `roles.read` - Read roles
- `roles.write` - Create/update roles
- `roles.delete` - Delete roles
- `permissions.read` - Read permissions
- `permissions.write` - Assign/remove permissions

---

## 👤 Default Admin Credentials

```
Email: admin@authgate.com
Password: Admin@123
```

**Admin has all permissions by default.**

---

## 🚨 Error Responses

### 400 Bad Request
```json
{
  "message": "Validation error message"
}
```

### 401 Unauthorized
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### 403 Forbidden
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

### 404 Not Found
```json
{
  "message": "Resource not found"
}
```

---

## 📝 Notes

1. **All authenticated endpoints require** `Authorization: Bearer {token}` header
2. **Tokens expire after 15 minutes** - use refresh token to get a new one
3. **Pagination** default: page=1, pageSize=10, max pageSize=100
4. **Soft Delete**: Users are not physically deleted, only marked as inactive (`IsActive=false`)
5. **Permission-based access**: Endpoints are protected by specific permissions, not just roles
6. **MFA Flow**:
   1. Enable MFA → Get QR code + recovery codes
   2. Scan QR code in authenticator app
   3. Verify with 6-digit code
   4. MFA now required for sensitive operations
