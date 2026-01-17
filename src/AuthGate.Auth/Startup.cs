using AuthGate.Auth;
using AuthGate.Auth.Infrastructure;
using AuthGate.Auth.Infrastructure.Services;
using AuthGate.Auth.Application;
using AuthGate.Auth.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using AuthGate.Auth.Infrastructure.Jobs;
using AuthGate.Auth.Infrastructure.Options;
using AuthGate.Auth.Middleware;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AuthGate.Auth;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        Env = env;
    }

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Env { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add Application and Infrastructure layers
        services.AddApplication();
        services.AddInfrastructure(Configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: "AuthGate.Auth"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                var otlpEndpoint = Configuration["OpenTelemetry:Otlp:Endpoint"];
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                metrics.AddPrometheusExporter();
            });

        services.AddIdentityService();

        // HttpContextAccessor
        services.AddHttpContextAccessor();

        // Permission-based Authorization
        services.AddPermissionAuthorization();

        // Rate Limiting
        services.AddRateLimitingPolicies();

        // Controllers
        services.AddControllers();

        services.Configure<AuditRetentionOptions>(Configuration.GetSection("AuditRetention"));
        services.AddHostedService<AuditRetentionHostedService>();
        
        // Outbox Processor for reliable async operations (Registration, etc.)
        services.Configure<OutboxProcessorOptions>(Configuration.GetSection(OutboxProcessorOptions.SectionName));
        services.AddHostedService<OutboxProcessorService>();

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<AuthGate.Auth.Infrastructure.Persistence.AuthDbContext>("database", tags: new[] { "ready" })
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "live" });

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? 
                new[] { "http://localhost:4200", "http://localhost:4300" };
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        // JWT Authentication with RSA
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var aspnetEnv = Configuration["ASPNETCORE_ENVIRONMENT"];
            var isDev = string.Equals(aspnetEnv, Environments.Development, StringComparison.OrdinalIgnoreCase);
            options.RequireHttpsMetadata = !isDev;
            options.SaveToken = true;
            
            // Configure token validation with RSA key resolver
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Get RSA key service and configure validation parameters
                    var rsaKeyService = context.HttpContext.RequestServices.GetRequiredService<RsaKeyService>();
                    context.Options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeyResolver = (_, _, kid, _) =>
                        {
                            // Allow validation against all non-pruned public keys to support key rotation
                            var keys = rsaKeyService.GetAllPublicParameters();
                            if (!string.IsNullOrWhiteSpace(kid))
                            {
                                keys = keys.Where(k => string.Equals(k.Kid, kid, StringComparison.Ordinal)).ToList();
                            }

                            return keys
                                .Select(k => (SecurityKey)new RsaSecurityKey(k.PublicParameters) { KeyId = k.Kid })
                                .ToList();
                        },
                        ValidateIssuer = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = Configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "AuthGate API",
                Version = "v1",
                Description = "Authentication and Authorization API with JWT, MFA, RBAC/PBAC",
                Contact = new OpenApiContact
                {
                    Name = "AuthGate",
                    Email = "support@authgate.com"
                }
            });

            // Add JWT authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        EnsureAuthGateLogDirectories();
        // Seed database in development
        if (env.IsDevelopment())
        {
            using var scope = app.ApplicationServices.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<AuthGate.Auth.Infrastructure.Persistence.DataSeeding.AuthDbSeeder>();
            seeder.SeedAsync().Wait();
            
            app.UseDeveloperExceptionPage();
        }

        var seedRolesConfigured = Configuration.GetValue<bool?>("Identity:SeedRoles");
        var seedRoles = seedRolesConfigured ?? true;

        if (!env.IsDevelopment() && seedRoles)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<AuthGate.Auth.Infrastructure.Persistence.DataSeeding.AuthDbSeeder>();
            seeder.EnsureRolesAndPermissionsAsync().Wait();
        }

        var swaggerEnabled = env.IsDevelopment() || Configuration.GetValue<bool>("Swagger:Enabled");
        if (env.IsDevelopment() || env.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthGate API v1");
                c.RoutePrefix = "swagger";
            });
        }

        var forceHttpsConfigured = Configuration.GetValue<bool?>("Security:ForceHttps");
        var forceHttps = forceHttpsConfigured ?? env.IsProduction();
        if (forceHttps)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
                context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
                context.Response.Headers.TryAdd("Content-Security-Policy", "frame-ancestors 'none';");
                return Task.CompletedTask;
            });

            await next();
        });

        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseSerilogRequestLogging();

        app.UseObservabilityEnrichment();
        
        app.UseRouting();

        app.UseCors("AllowFrontend");

        // Rate Limiting must be after UseRouting and before UseAuthentication
        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            endpoints.MapPrometheusScrapingEndpoint("/metrics").RequireAuthorization();
            
            // Health Check Endpoints
            endpoints.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString(),
                        timestamp = DateTime.UtcNow,
                        service = "AuthGate",
                        checks = report.Entries.Select(e => new
                        {
                            name = e.Key,
                            status = e.Value.Status.ToString(),
                            description = e.Value.Description,
                            duration = e.Value.Duration.TotalMilliseconds
                        })
                    });
                    await context.Response.WriteAsync(result);
                }
            }).AllowAnonymous();

            // Public SaaS health endpoint (Uptime-Kuma)
            endpoints.MapHealthChecks("/healthz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => true
            }).AllowAnonymous();

            // Readiness probe
            endpoints.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            }).AllowAnonymous();

            endpoints.MapHealthChecks("/healthz/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            }).AllowAnonymous();

            // Liveness probe
            endpoints.MapHealthChecks("/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            }).AllowAnonymous();

            endpoints.MapHealthChecks("/healthz/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("live")
            }).AllowAnonymous();
        });
    }

    private void EnsureAuthGateLogDirectories()
    {
        // 1) Récupère la variable
        var home = Environment.GetEnvironmentVariable("AUTHGATE_HOME");

        // 2) Si elle n’existe pas au runtime, on la force (au niveau Process)
        //    Important : mets bien un trailing backslash : E:\ (pas E:)
        if (string.IsNullOrWhiteSpace(home))
        {
            home = @"E:\";
            Environment.SetEnvironmentVariable("AUTHGATE_HOME", home, EnvironmentVariableTarget.Process);
        }

        // 3) Crée les dossiers attendus par tes sinks fichier
        var authGateDir = Path.Combine(home, "log", "AuthGate");
        var efDir = Path.Combine(authGateDir, "EntityFramework");

        Directory.CreateDirectory(authGateDir);
        Directory.CreateDirectory(efDir);
    }
}

