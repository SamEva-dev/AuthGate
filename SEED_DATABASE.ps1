# Script pour seed la base de données AuthGate
Write-Host "=== Seeding AuthGate Database ===" -ForegroundColor Cyan

Set-Location "E:\Gestion Immobilier\AuthGate"

# Créer un programme temporaire pour le seeding
$seedCode = @"
using AuthGate.Auth.Infrastructure;
using AuthGate.Auth.Infrastructure.Persistence;
using AuthGate.Auth.Infrastructure.Persistence.DataSeeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("src/AuthGate.Auth/appsettings.json")
    .Build();

var services = new ServiceCollection();
services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
    
services.AddInfrastructure(configuration);

var serviceProvider = services.BuildServiceProvider();

using (var scope = serviceProvider.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<AuthDbSeeder>();
    await seeder.SeedAsync();
    Console.WriteLine("✅ Seeding completed!");
}
"@

$seedCode | Out-File -FilePath "SeedTool.cs" -Encoding UTF8

# Créer un projet temporaire
$projectContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="src\AuthGate.Auth.Infrastructure\AuthGate.Auth.Infrastructure.csproj" />
  </ItemGroup>
</Project>
"@

$projectContent | Out-File -FilePath "SeedTool.csproj" -Encoding UTF8

Write-Host "Building seed tool..." -ForegroundColor Yellow
dotnet build SeedTool.csproj

Write-Host "Running seeder..." -ForegroundColor Yellow
dotnet run --project SeedTool.csproj

# Cleanup
Remove-Item "SeedTool.cs" -ErrorAction SilentlyContinue
Remove-Item "SeedTool.csproj" -ErrorAction SilentlyContinue
Remove-Item "obj" -Recurse -ErrorAction SilentlyContinue
Remove-Item "bin" -Recurse -ErrorAction SilentlyContinue

Write-Host "`nDone! Try login again with admin@authgate.com / Admin@123" -ForegroundColor Green
