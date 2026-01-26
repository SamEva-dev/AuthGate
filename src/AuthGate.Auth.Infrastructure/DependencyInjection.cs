using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using AuthGate.Auth.Infrastructure.Persistence;
using AuthGate.Auth.Infrastructure.Persistence.Repositories;
using AuthGate.Auth.Infrastructure.Repositories;
using AuthGate.Auth.Infrastructure.Services;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using AuthGate.Auth.Application.Common.Security;
using AuthGate.Auth.Infrastructure.Security;
using LocaGuest.Emailing.Registration;
using LocaGuest.Emailing.Workers;

namespace AuthGate.Auth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"]?.ToLowerInvariant() ?? "postgresql";

        // Main Database (Auth)
        services.AddDbContext<AuthDbContext>(options =>
        {
            if (provider == "inmemory")
            {
                var dbName = configuration["Database:InMemory:AuthDbName"];
                options.UseInMemoryDatabase(string.IsNullOrWhiteSpace(dbName) ? "AuthGate_InMemory_Auth" : dbName);
            }
            else if (provider == "sqlite")
            {
                throw new InvalidOperationException("SQLite is no longer supported. Set Database:Provider to 'postgresql'.");
            }
            else
            {
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                string connectionString;
                
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                    // Format: postgres://user:password@host:port/database?sslmode
                    var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                    var userInfo = uri.UserInfo.Split(':');
                    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Disable";
                }
                else
                {
                    connectionString = configuration.GetConnectionString("DefaultConnection_Auth");
                }
                
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));
            }
        });

        if (provider != "inmemory")
        {
            // LocaGuest.Emailing (queue + worker) - uses same Postgres DB as AuthGate
            services.AddLocaGuestEmailing(configuration, db =>
            {
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                string connectionString;

                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                    var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                    var userInfo = uri.UserInfo.Split(':');
                    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Disable";
                }
                else
                {
                    connectionString = configuration.GetConnectionString("DefaultConnection_Auth");
                }

                db.UsePostgres(connectionString, migrationsAssembly: typeof(DependencyInjection).Assembly.FullName);
            });
            services.AddHostedService<EmailDispatcherWorker>();
        }
        
        // Register AuthDbContext as DbContext for handler injection
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<AuthDbContext>());

        // Audit Database (Separate)
        services.AddDbContext<AuditDbContext>(options =>
        {
            if (provider == "inmemory")
            {
                var dbName = configuration["Database:InMemory:AuditDbName"];
                options.UseInMemoryDatabase(string.IsNullOrWhiteSpace(dbName) ? "AuthGate_InMemory_Audit" : dbName);
            }
            else if (provider == "sqlite")
            {
                throw new InvalidOperationException("SQLite is no longer supported. Set Database:Provider to 'postgresql'.");
            }
            else
            {
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                string connectionString;
                
                if (!string.IsNullOrEmpty(databaseUrl))
                {
                    // Parse DATABASE_URL manually to avoid malformed sslmode parameter
                    var uri = new Uri(databaseUrl.Split('?')[0]); // Remove query params
                    var userInfo = uri.UserInfo.Split(':');
                    var dbName = uri.AbsolutePath.TrimStart('/') + "_audit";
                    connectionString = $"Host={uri.Host};Port={uri.Port};Database={dbName};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Disable";
                }
                else
                {
                    connectionString = configuration.GetConnectionString("AuditConnection_Auth");
                }
                
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName));
            }
        });

        services.AddScoped<AuthGate.Auth.Application.Common.Interfaces.IAuditDbContext>(sp => sp.GetRequiredService<AuditDbContext>());

        // Repositories (custom)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserOrganizationRepository, UserOrganizationRepository>();
        services.AddScoped<IUserAppAccessRepository, UserAppAccessRepository>();
        services.AddScoped<IMfaSecretRepository, MfaSecretRepository>();
        services.AddScoped<IRecoveryCodeRepository, RecoveryCodeRepository>();
        services.AddScoped<ITrustedDeviceRepository, TrustedDeviceRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Audit repository (separate DB)
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // RSA Key Service (Singleton for key persistence during app lifetime)
        services.AddSingleton<RsaKeyService>();
        
        // Services
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<Application.Common.Interfaces.IPasswordHasher, Services.PasswordHasher>();
        services.AddScoped<ITotpService, TotpService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IDeviceFingerprintService, DeviceFingerprintService>();
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

        services.Configure<LocaGuestOptions>(configuration.GetSection("LocaGuest"));
        services.Configure<MachineTokenOptions>(configuration.GetSection("LocaGuest:MachineToken"));
        services.AddSingleton<IMachineTokenProvider, MachineTokenProvider>();

        services
            .AddHttpClient<ILocaGuestProvisioningClient, LocaGuestProvisioningClient>((sp, client) =>
            {
                var opt = sp.GetRequiredService<IOptions<LocaGuestOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(opt.ApiBaseUrl))
                    client.BaseAddress = new Uri(opt.ApiBaseUrl.TrimEnd('/') + "/");

                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddStandardResilienceHandler(standard =>
            {
                var opt = configuration.GetSection("LocaGuest:Resilience").Get<ResilienceOptions>() ?? new ResilienceOptions();

                standard.AttemptTimeout.Timeout = TimeSpan.FromSeconds(opt.AttemptTimeoutSeconds);
                standard.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(opt.TotalTimeoutSeconds);

                standard.Retry.MaxRetryAttempts = opt.MaxRetries;
                standard.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                standard.Retry.UseJitter = true;

                standard.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(opt.BreakDurationSeconds);
                standard.CircuitBreaker.MinimumThroughput = opt.MinimumThroughput;
                standard.CircuitBreaker.FailureRatio = opt.FailureRatio;
            });

        services
            .AddHttpClient<ILocaGuestInvitationProvisioningClient, LocaGuestInvitationProvisioningClient>((sp, client) =>
            {
                var opt = sp.GetRequiredService<IOptions<LocaGuestOptions>>().Value;
                if (!string.IsNullOrWhiteSpace(opt.ApiBaseUrl))
                    client.BaseAddress = new Uri(opt.ApiBaseUrl.TrimEnd('/') + "/");

                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            .AddStandardResilienceHandler(standard =>
            {
                var opt = configuration.GetSection("LocaGuest:Resilience").Get<ResilienceOptions>() ?? new ResilienceOptions();

                standard.AttemptTimeout.Timeout = TimeSpan.FromSeconds(opt.AttemptTimeoutSeconds);
                standard.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(opt.TotalTimeoutSeconds);

                standard.Retry.MaxRetryAttempts = opt.MaxRetries;
                standard.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                standard.Retry.UseJitter = true;

                standard.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(opt.BreakDurationSeconds);
                standard.CircuitBreaker.MinimumThroughput = opt.MinimumThroughput;
                standard.CircuitBreaker.FailureRatio = opt.FailureRatio;
            });
        
        // HttpContext Accessor
        services.AddHttpContextAccessor();
        
        // Multi-Tenant Services (NEW)
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<TenantContext>();
        services.AddScoped<IOrganizationContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AuthDbContext>());

        // Data Seeding
        services.AddScoped<Persistence.DataSeeding.AuthDbSeeder>();

        return services;
    }
}
