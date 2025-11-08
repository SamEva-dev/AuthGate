# Script de test AuthGate RS256
Write-Host "=== Test AuthGate RS256 ===" -ForegroundColor Cyan

# 1. Build
Write-Host "`n[1/4] Building AuthGate..." -ForegroundColor Yellow
Set-Location "E:\Gestion Immobilier\AuthGate"
dotnet build AuthGate.sln --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Fermez Visual Studio et réessayez." -ForegroundColor Red
    exit 1
}

Write-Host "Build OK!" -ForegroundColor Green

# 2. Lancer l'API en background
Write-Host "`n[2/4] Starting AuthGate API..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/AuthGate.Auth/AuthGate.Auth.csproj --no-build --configuration Release" -PassThru -NoNewWindow

# Attendre que l'API démarre
Write-Host "Waiting for API to start (10s)..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# 3. Tester JWKS
Write-Host "`n[3/4] Testing JWKS endpoint..." -ForegroundColor Yellow
try {
    $jwks = Invoke-RestMethod -Uri "http://localhost:8080/.well-known/jwks.json" -Method Get
    Write-Host "JWKS Response:" -ForegroundColor Green
    $jwks | ConvertTo-Json -Depth 10
    
    if ($jwks.keys.Count -gt 0) {
        Write-Host "`nJWKS OK! Key ID: $($jwks.keys[0].kid)" -ForegroundColor Green
        Write-Host "Algorithm: $($jwks.keys[0].alg)" -ForegroundColor Green
    }
} catch {
    Write-Host "JWKS request failed: $_" -ForegroundColor Red
}

# 4. Tester Login
Write-Host "`n[4/4] Testing Login with RS256..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@authgate.com"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    
    Write-Host "`nLogin successful!" -ForegroundColor Green
    Write-Host "Access Token (first 50 chars): $($loginResponse.accessToken.Substring(0, 50))..." -ForegroundColor Green
    
    # Décoder le JWT header
    $tokenParts = $loginResponse.accessToken.Split('.')
    $header = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[0] + "=="))
    $payload = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[1] + "=="))
    
    Write-Host "`nJWT Header:" -ForegroundColor Cyan
    $header | ConvertFrom-Json | ConvertTo-Json
    
    Write-Host "`nJWT Payload (permissions):" -ForegroundColor Cyan
    $payloadObj = $payload | ConvertFrom-Json
    Write-Host "User: $($payloadObj.email)" -ForegroundColor White
    Write-Host "Roles: $($payloadObj.role -join ', ')" -ForegroundColor White
    Write-Host "Permissions: $($payloadObj.permission -join ', ')" -ForegroundColor White
    
    # Vérifier que c'est bien RS256
    $headerObj = $header | ConvertFrom-Json
    if ($headerObj.alg -eq "RS256") {
        Write-Host "`n✅ JWT signed with RS256!" -ForegroundColor Green
    } else {
        Write-Host "`n❌ JWT NOT signed with RS256! Algorithm: $($headerObj.alg)" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Login failed: $_" -ForegroundColor Red
}

# 5. Cleanup
Write-Host "`n[Cleanup] Stopping API..." -ForegroundColor Yellow
Stop-Process -Id $apiProcess.Id -Force
Write-Host "Done!" -ForegroundColor Green
