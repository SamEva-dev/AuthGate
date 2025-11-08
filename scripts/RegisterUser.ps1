# Registers a demo user in AuthGate
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080'
$body = @{ 
  email = 'test@test.com'
  password = 'Test@123'
  confirmPassword = 'Test@123'
  firstName = 'Test'
  lastName = 'User'
} | ConvertTo-Json

try {
  $res = Invoke-RestMethod -Uri "$base/api/Register" -Method Post -Body $body -ContentType 'application/json'
  Write-Host 'User registered.' -ForegroundColor Green
  $res | ConvertTo-Json -Depth 5
} catch {
  Write-Warning "Register may have failed or user already exists: $($_.Exception.Message)"
}
