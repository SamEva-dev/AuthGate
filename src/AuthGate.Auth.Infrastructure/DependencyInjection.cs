using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Application.Services.Email;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using AuthGate.Auth.Infrastructure.Persistence;
using AuthGate.Auth.Infrastructure.Persistence.Repositories;
using AuthGate.Auth.Infrastructure.Repositories;
using AuthGate.Auth.Infrastructure.Services;
using AuthGate.Auth.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AuthGate.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToLowerInvariant();

        // Main Database (Auth)
        services.AddDbContext<AuthDbContext>(options =>
        {
            if (provider == "sqlite")
            {
                options.UseSqlite(
                    configuration.GetConnectionString("SqliteConnection"),
                    b => b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));
            }
            else
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));
            }
        });
        
        // Register AuthDbContext as DbContext for handler injection
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AuthDbContext>());

        // Audit Database (Separate)
        services.AddDbContext<AuditDbContext>(options =>
        {
            if (provider == "sqlite")
            {
                options.UseSqlite(
                    configuration.GetConnectionString("SqliteAuditConnection"),
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName));
            }
            else
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("AuditConnection"),
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName));
            }
        });

        // Repositories (custom)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMfaSecretRepository, MfaSecretRepository>();
        services.AddScoped<IRecoveryCodeRepository, RecoveryCodeRepository>();
        services.AddScoped<ITrustedDeviceRepository, TrustedDeviceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Audit repository (separate DB)
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // RSA Key Service (Singleton for key persistence during app lifetime)
        services.AddSingleton<RsaKeyService>();
        
        // Services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<Application.Common.Interfaces.IPasswordHasher, Services.PasswordHasher>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IDeviceFingerprintService, DeviceFingerprintService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<Application.Common.Interfaces.IHttpContextAccessor, HttpContextAccessorService>();
        
        // HttpClient for external API calls with Polly resilience policies
        services.AddHttpClient("LocaGuestApi", client =>
        {
            var baseUrl = configuration["HttpClients:LocaGuestApi:BaseUrl"] ?? "https://localhost:5001";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddTransientHttpErrorPolicy(policyBuilder => 
            policyBuilder.WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))) // Exponential backoff: 2s, 4s, 8s
        .AddPolicyHandler(Polly.Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10))); // Circuit timeout per request
        
        // HttpContext Accessor
        services.AddHttpContextAccessor();
        
        // Multi-Tenant Services (NEW)
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());

        // Data Seeding
        services.AddScoped<Persistence.DataSeeding.AuthDbSeeder>();

        return services;
    }
}
