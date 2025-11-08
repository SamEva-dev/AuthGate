# Logs in a user against AuthGate and prints token info
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080'
$body = @{ 
  email = 'test@test.com'
  password = 'Test@123'
} | ConvertTo-Json

$res = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method Post -Body $body -ContentType 'application/json'

Write-Host "Login OK" -ForegroundColor Green
$token = $res.accessToken

# Decode JWT header (Base64Url)
function Decode-Base64Url([string]$b64u) {
  $s = $b64u.Replace('-', '+').Replace('_', '/')
  switch ($s.Length % 4) { 2 { $s += '==' } 3 { $s += '=' } default { } }
  return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($s))
}

$parts = $token.Split('.')
$headerJson = Decode-Base64Url $parts[0]
$payloadJson = Decode-Base64Url $parts[1]

Write-Host "Header:" -ForegroundColor Cyan
$headerJson | ConvertFrom-Json | Format-List

Write-Host "Payload (email/roles/permissions):" -ForegroundColor Cyan
$p = $payloadJson | ConvertFrom-Json
"email: $($p.email)"
"roles: $($p.role -join ', ')"
"permissions: $($p.permission -join ', ')"

# Output minimal result for frontend test
[PSCustomObject]@{
  accessToken = $res.accessToken
  refreshToken = $res.refreshToken
  expiresAt = $res.expiresAt
} | ConvertTo-Json -Depth 5
