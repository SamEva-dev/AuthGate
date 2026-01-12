using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using AuthGate.Auth;
using AuthGate.Auth.Application;
using AuthGate.Auth.Infrastructure;
using Npgsql;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

try
{
    Log.Information("*** STARTUP ***");

    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
    {
        try
        {
            var defaultCs = builder.Configuration.GetConnectionString("DefaultConnection_Auth");
            var auditCs = builder.Configuration.GetConnectionString("AuditConnection_Auth");

            var defaultHasPwd = !string.IsNullOrWhiteSpace(defaultCs) && !string.IsNullOrWhiteSpace(new NpgsqlConnectionStringBuilder(defaultCs).Password);
            var auditHasPwd = !string.IsNullOrWhiteSpace(auditCs) && !string.IsNullOrWhiteSpace(new NpgsqlConnectionStringBuilder(auditCs).Password);

            Log.Information("Startup DB config (Development): DefaultConnection_Auth has password={DefaultHasPwd}, AuditConnection_Auth has password={AuditHasPwd}", defaultHasPwd, auditHasPwd);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Startup DB config (Development): unable to parse connection strings");
        }
    }

    static string ResolveHomeDirectory(string envVarName, string appFolderName)
    {
        var fromEnv = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = AppContext.BaseDirectory;
        }

        var resolved = Path.Combine(baseDir, appFolderName);
        Environment.SetEnvironmentVariable(envVarName, resolved, EnvironmentVariableTarget.Process);
        return resolved;
    }

    static void ConfigureSerilogFilePaths(WebApplicationBuilder b, string homeEnvVar, string appFolder)
    {
        var home = ResolveHomeDirectory(homeEnvVar, appFolder);

        var appLogPath = Path.Combine(home, "log", "AuthGate", "AuthGateService_log.txt");
        var efLogPath = Path.Combine(home, "log", "AuthGate", "EntityFramework", "EntityFramework_log.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(appLogPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(efLogPath)!);

        b.Configuration["Serilog:WriteTo:1:Args:configureLogger:WriteTo:0:Args:path"] = appLogPath;
        b.Configuration["Serilog:WriteTo:2:Args:configureLogger:WriteTo:0:Args:path"] = efLogPath;
    }

    ConfigureSerilogFilePaths(builder, "AUTHGATE_HOME", "AuthGate");
    
    // Use Serilog
    builder.Host.UseSerilog((ctx, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services));

    // Use Startup class for service configuration
    var startup = new Startup(builder.Configuration, builder.Environment);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    // Apply migrations
    if (!app.Environment.IsEnvironment("Testing"))
    {
        AuthGate.Auth.MigrationManager.ApplyMigrations(app);
    }

    // Use Startup class for middleware configuration
    startup.Configure(app, app.Environment);

    Log.Information("AuthGate API starting...");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
