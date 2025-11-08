$filePath = "src\AuthGate.Auth\Program.cs"
$content = Get-Content $filePath -Raw

# Remplacer .AddDefaultTokenProviders(); par .AddDefaultTokenProviders().AddSignInManager();
$newContent = $content -replace '\.AddDefaultTokenProviders\(\);', '.AddDefaultTokenProviders().AddSignInManager();'

Set-Content -Path $filePath -Value $newContent -NoNewline

Write-Host "Program.cs modifie avec succes!" -ForegroundColor Green
