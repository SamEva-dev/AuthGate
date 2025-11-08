# Login as admin and print token info
$ErrorActionPreference = 'Stop'
$base = 'http://localhost:8080'
$body = @{ 
  email = 'admin@authgate.com'
  password = 'Admin@123'
} | ConvertTo-Json

$res = Invoke-RestMethod -Uri "$base/api/Auth/login" -Method Post -Body $body -ContentType 'application/json'

Write-Host "Login OK" -ForegroundColor Green
$token = $res.accessToken

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

[PSCustomObject]@{
  accessToken = $res.accessToken
  refreshToken = $res.refreshToken
  expiresIn = $res.expiresIn
} | ConvertTo-Json -Depth 5
