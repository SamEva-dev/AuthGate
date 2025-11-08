# Fix rapide: Login 401 Unauthorized

## Problème
Le test RS256 fonctionne (JWKS OK) mais le login échoue car **l'admin n'existe pas en base**.

## ✅ Solution Option 1: Via Register API (RAPIDE)

1. **Lancer l'API** (Terminal):
```powershell
cd "E:\Gestion Immobilier\AuthGate"
dotnet run --project src/AuthGate.Auth/AuthGate.Auth.csproj
```

2. **Créer l'admin via Register** (nouveau terminal):
```powershell
$body = @{
    email = "admin@authgate.com"
    password = "Admin@123"
    confirmPassword = "Admin@123"
    firstName = "Admin"
    lastName = "User"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:8080/api/Register" -Method Post -Body $body -ContentType "application/json"
```

3. **Relancer le test**:
```powershell
.\TEST_RS256_IMPROVED.ps1
```

---

## ✅ Solution Option 2: Via SQL (si Register est protégé)

1. **Ouvrir pgAdmin ou psql**

2. **Se connecter à la base `AuthGate`**

3. **Exécuter le script SQL**: `CREATE_ADMIN.sql` (créé pour toi)

Ou en une ligne PowerShell:
```powershell
psql -U postgres -d AuthGate -f CREATE_ADMIN.sql
```

Mot de passe PostgreSQL: `locaguest`

---

## ✅ Solution Option 3: Tester avec un nouveau user

Au lieu de `admin@authgate.com`, créer un nouvel utilisateur:

```powershell
# 1. API tourne
dotnet run --project src/AuthGate.Auth/AuthGate.Auth.csproj

# 2. Nouveau terminal: Register
$body = @{
    email = "test@test.com"
    password = "Test@123"
    confirmPassword = "Test@123"
    firstName = "Test"
    lastName = "User"
} | ConvertTo-Json

$newUser = Invoke-RestMethod -Uri "http://localhost:8080/api/Register" -Method Post -Body $body -ContentType "application/json"

# 3. Login avec ce user
$loginBody = @{
    email = "test@test.com"
    password = "Test@123"
} | ConvertTo-Json

$login = Invoke-RestMethod -Uri "http://localhost:8080/api/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"

# 4. Vérifier le JWT
$token = $login.accessToken
$headerB64 = $token.Split('.')[0]
while ($headerB64.Length % 4 -ne 0) { $headerB64 += "=" }
$header = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($headerB64.Replace('-', '+').Replace('_', '/')))
$header | ConvertFrom-Json
# Devrait montrer: "alg": "RS256" ✅
```

---

## 🎯 Résumé

### Ce qui marche déjà:
✅ Build AuthGate  
✅ API démarre  
✅ **JWKS endpoint fonctionne** (`/.well-known/jwks.json`)  
✅ **Clé RSA générée** (kid présent, modulus/exponent valides)  
✅ **Algorithm = RS256** confirmé  

### Ce qui manque:
❌ User admin en base de données

### Quick Fix:
**Option la plus rapide**: Register via API

```powershell
# Terminal 1
dotnet run --project src/AuthGate.Auth/AuthGate.Auth.csproj

# Terminal 2 (attendre 5s que l'API démarre)
$reg = @{email="admin@authgate.com"; password="Admin@123"; confirmPassword="Admin@123"; firstName="Admin"; lastName="User"} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8080/api/Register" -Method Post -Body $reg -ContentType "application/json"

# Maintenant login fonctionne
$login = @{email="admin@authgate.com"; password="Admin@123"} | ConvertTo-Json
$result = Invoke-RestMethod -Uri "http://localhost:8080/api/Auth/login" -Method Post -Body $login -ContentType "application/json"

# Vérifier JWT RS256
$result.accessToken.Split('.')[0] | ForEach-Object {
    $h = $_ + "="
    while ($h.Length % 4 -ne 0) { $h += "=" }
    [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($h.Replace('-', '+').Replace('_', '/')))
} | ConvertFrom-Json
```

Si ça affiche `"alg": "RS256"` → **MIGRATION RÉUSSIE !** 🎉

---

## 📝 Note importante

Le JWKS fonctionne parfaitement. C'est juste que la base est vide. Une fois le user créé (register ou SQL), le login RS256 fonctionnera immédiatement.
