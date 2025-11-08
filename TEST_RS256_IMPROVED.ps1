# Script de test AuthGate RS256 (version améliorée)
Write-Host "=== Test AuthGate RS256 ===" -ForegroundColor Cyan

# 1. Build
Write-Host "`n[1/5] Building AuthGate..." -ForegroundColor Yellow
Set-Location "E:\Gestion Immobilier\AuthGate"
dotnet build AuthGate.sln --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Fermez Visual Studio et réessayez." -ForegroundColor Red
    exit 1
}

Write-Host "Build OK!" -ForegroundColor Green

# 2. Lancer l'API en background
Write-Host "`n[2/5] Starting AuthGate API..." -ForegroundColor Yellow
$apiProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run --project src/AuthGate.Auth/AuthGate.Auth.csproj --no-build --configuration Release" `
    -PassThru -NoNewWindow -RedirectStandardOutput "api-output.log" -RedirectStandardError "api-error.log"

# 3. Attendre que l'API réponde (max 60s)
Write-Host "Waiting for API to be ready..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$apiReady = $false

while ($attempt -lt $maxAttempts -and -not $apiReady) {
    $attempt++
    Start-Sleep -Seconds 2
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $apiReady = $true
            Write-Host "API is ready! (attempt $attempt)" -ForegroundColor Green
        }
    } catch {
        Write-Host "." -NoNewline
    }
}

if (-not $apiReady) {
    Write-Host "`nAPI failed to start after 60s. Check logs:" -ForegroundColor Red
    Write-Host "- api-output.log" -ForegroundColor Yellow
    Write-Host "- api-error.log" -ForegroundColor Yellow
    Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

# 4. Tester JWKS
Write-Host "`n[3/5] Testing JWKS endpoint..." -ForegroundColor Yellow
try {
    $jwks = Invoke-RestMethod -Uri "http://localhost:8080/.well-known/jwks.json" -Method Get
    Write-Host "`nJWKS Response:" -ForegroundColor Green
    $jwks | ConvertTo-Json -Depth 10
    
    if ($jwks.keys.Count -gt 0) {
        $kid = $jwks.keys[0].kid
        $alg = $jwks.keys[0].alg
        Write-Host "`n✅ JWKS OK!" -ForegroundColor Green
        Write-Host "   Key ID: $kid" -ForegroundColor White
        Write-Host "   Algorithm: $alg" -ForegroundColor White
        Write-Host "   Key Type: $($jwks.keys[0].kty)" -ForegroundColor White
    }
} catch {
    Write-Host "❌ JWKS request failed: $_" -ForegroundColor Red
}

# 5. Tester Login
Write-Host "`n[4/5] Testing Login with RS256..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@authgate.com"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    
    Write-Host "`n✅ Login successful!" -ForegroundColor Green
    Write-Host "   Access Token (first 50 chars): $($loginResponse.accessToken.Substring(0, [Math]::Min(50, $loginResponse.accessToken.Length)))..." -ForegroundColor White
    Write-Host "   Expires at: $($loginResponse.expiresAt)" -ForegroundColor White
    
    # Décoder le JWT header et payload
    $tokenParts = $loginResponse.accessToken.Split('.')
    
    # Ajouter padding si nécessaire
    $headerB64 = $tokenParts[0]
    while ($headerB64.Length % 4 -ne 0) { $headerB64 += "=" }
    $payloadB64 = $tokenParts[1]
    while ($payloadB64.Length % 4 -ne 0) { $payloadB64 += "=" }
    
    # Décoder
    $headerBytes = [System.Convert]::FromBase64String($headerB64.Replace('-', '+').Replace('_', '/'))
    $payloadBytes = [System.Convert]::FromBase64String($payloadB64.Replace('-', '+').Replace('_', '/'))
    
    $header = [System.Text.Encoding]::UTF8.GetString($headerBytes)
    $payload = [System.Text.Encoding]::UTF8.GetString($payloadBytes)
    
    Write-Host "`nJWT Header:" -ForegroundColor Cyan
    $headerObj = $header | ConvertFrom-Json
    $headerObj | Format-List
    
    Write-Host "JWT Payload:" -ForegroundColor Cyan
    $payloadObj = $payload | ConvertFrom-Json
    Write-Host "   User: $($payloadObj.email)" -ForegroundColor White
    Write-Host "   Subject: $($payloadObj.sub)" -ForegroundColor White
    Write-Host "   Roles: $($payloadObj.role -join ', ')" -ForegroundColor White
    Write-Host "   Permissions: $($payloadObj.permission -join ', ')" -ForegroundColor White
    Write-Host "   MFA Enabled: $($payloadObj.mfa_enabled)" -ForegroundColor White
    Write-Host "   Issuer: $($payloadObj.iss)" -ForegroundColor White
    Write-Host "   Audience: $($payloadObj.aud)" -ForegroundColor White
    
    # Vérifier RS256
    if ($headerObj.alg -eq "RS256") {
        Write-Host "`n🎉 SUCCESS: JWT is signed with RS256!" -ForegroundColor Green
        Write-Host "   Kid in token: $($headerObj.kid)" -ForegroundColor White
        Write-Host "`n✅ AuthGate RS256 migration COMPLETED!" -ForegroundColor Green -BackgroundColor DarkGreen
    } else {
        Write-Host "`n❌ FAILED: JWT is NOT signed with RS256!" -ForegroundColor Red
        Write-Host "   Algorithm found: $($headerObj.alg)" -ForegroundColor Yellow
    }
    
    # Tester un endpoint protégé
    Write-Host "`n[5/5] Testing protected endpoint..." -ForegroundColor Yellow
    try {
        $headers = @{
            Authorization = "Bearer $($loginResponse.accessToken)"
        }
        $users = Invoke-RestMethod -Uri "http://localhost:8080/api/Permissions" -Method Get -Headers $headers
        Write-Host "✅ Protected endpoint works! Found $($users.Count) permissions" -ForegroundColor Green
    } catch {
        Write-Host "❌ Protected endpoint failed: $_" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Yellow
}

# 6. Cleanup
Write-Host "`n[Cleanup] Stopping API..." -ForegroundColor Yellow
Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
Remove-Item -Path "api-output.log" -ErrorAction SilentlyContinue
Remove-Item -Path "api-error.log" -ErrorAction SilentlyContinue

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
