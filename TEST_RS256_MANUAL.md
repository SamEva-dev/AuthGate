# Test Manuel AuthGate RS256

## Prérequis
1. **Fermer Visual Studio** (sinon DLL bloquées)
2. Avoir PostgreSQL qui tourne (si migrations pas encore appliquées)

---

## Option 1: Script PowerShell automatique

```powershell
cd "E:\Gestion Immobilier\AuthGate"
.\TEST_RS256.ps1
```

Le script fait:
1. Build AuthGate
2. Lance l'API
3. Teste JWKS
4. Teste Login
5. Affiche le JWT décodé
6. Vérifie RS256

---

## Option 2: Tests manuels

### 1. Build & Run

**Terminal 1** (Build):
```powershell
cd "E:\Gestion Immobilier\AuthGate"
dotnet build AuthGate.sln --configuration Release
```

**Terminal 2** (Run):
```powershell
cd "E:\Gestion Immobilier\AuthGate"
dotnet run --project src/AuthGate.Auth/AuthGate.Auth.csproj --no-build --configuration Release
```

Attendre le message:
```
Now listening on: http://localhost:8080
```

---

### 2. Test JWKS

**Terminal 3** (Test):
```powershell
# PowerShell
Invoke-RestMethod -Uri "http://localhost:8080/.well-known/jwks.json" -Method Get | ConvertTo-Json -Depth 10
```

**Ou avec curl**:
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
      "n": "long_base64url_modulus...",
      "e": "AQAB"
    }
  ]
}
```

✅ **Vérifications**:
- `alg` = "RS256"
- `kid` existe (Key ID)
- `n` et `e` sont présents (modulus + exponent)

---

### 3. Test Login

```powershell
# PowerShell
$body = @{
    email = "admin@authgate.com"
    password = "Admin@123"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/Auth/login" -Method Post -Body $body -ContentType "application/json"
```

**Ou avec curl**:
```bash
curl -X POST http://localhost:8080/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@authgate.com","password":"Admin@123"}'
```

**Réponse attendue**:
```json
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "base64...",
  "expiresAt": "2025-11-02T20:00:00Z"
}
```

---

### 4. Vérifier JWT sur jwt.io

1. Copier le `accessToken`
2. Aller sur https://jwt.io
3. Coller le token

**Header doit montrer**:
```json
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "abc123..."
}
```

✅ **Si c'est RS256 → Migration réussie !**

**Payload doit contenir**:
```json
{
  "sub": "user-guid",
  "email": "admin@authgate.com",
  "role": ["Admin"],
  "permission": ["users.read", "users.write", ...],
  "mfa_enabled": "false",
  "exp": 1234567890,
  "iss": "AuthGate",
  "aud": "AuthGate"
}
```

---

## Vérifications de sécurité

### ✅ Token RS256
```powershell
# Décoder header
$token = "VOTRE_TOKEN_ICI"
$headerB64 = $token.Split('.')[0]
$header = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($headerB64 + "=="))
$header | ConvertFrom-Json
```

Doit afficher:
```json
{
  "alg": "RS256",
  "typ": "JWT",
  "kid": "..."
}
```

### ✅ Claims présents
```powershell
$payloadB64 = $token.Split('.')[1]
$payload = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payloadB64 + "=="))
$payload | ConvertFrom-Json | Select-Object email, role, permission
```

### ✅ JWKS accessible publiquement
- Aucune authentification requise
- Format JSON standard
- Clé publique RSA exposée

---

## Troubleshooting

### Erreur: "DLL locked"
**Solution**: Fermer Visual Studio, puis:
```powershell
Get-Process | Where-Object { $_.Name -like "*AuthGate*" } | Stop-Process -Force
dotnet build AuthGate.sln --force
```

### Erreur: "Connection refused"
**Solution**: Vérifier que l'API tourne:
```powershell
netstat -an | Select-String "8080"
```
Devrait montrer: `LISTENING` sur port 8080

### JWT toujours en HS256
**Solution**: 
1. Vérifier que `RsaKeyService` est bien Singleton dans DI
2. Vérifier que `JwtService` utilise `_rsaKeyService.GetSigningKey()`
3. Redémarrer l'API (nouvelle clé RSA générée)

### JWKS vide ou erreur
**Solution**:
1. Vérifier route dans `JwksController.cs`: `[Route(".well-known")]`
2. Vérifier que controller est public
3. Checker logs Serilog pour erreurs

---

## Commandes rapides (copy-paste)

### Build + Run + Test tout-en-un:
```powershell
cd "E:\Gestion Immobilier\AuthGate"

# Build
dotnet build AuthGate.sln

# Run en background
Start-Process -FilePath "dotnet" -ArgumentList "run --project src/AuthGate.Auth/AuthGate.Auth.csproj --no-build" -NoNewWindow

# Attendre 10s
Start-Sleep -Seconds 10

# Test JWKS
Invoke-RestMethod -Uri "http://localhost:8080/.well-known/jwks.json"

# Test Login
$body = '{"email":"admin@authgate.com","password":"Admin@123"}'
Invoke-RestMethod -Uri "http://localhost:8080/api/Auth/login" -Method Post -Body $body -ContentType "application/json"
```

---

## Résultat attendu

Si tout est OK:
```
✅ Build successful
✅ API started on http://localhost:8080
✅ JWKS endpoint returns RSA public key
✅ Login returns JWT token
✅ JWT header shows "alg": "RS256"
✅ JWT payload contains permissions
```

**🎉 AuthGate RS256 migration completed!**

Prochaine étape: Configurer LocaGuest.API pour valider ces JWT via JWKS.
