using Serilog;
using Serilog.Events;
using AuthGate.Auth;
using AuthGate.Auth.Application;
using AuthGate.Auth.Infrastructure;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("*** STARTUP ***");

    var builder = WebApplication.CreateBuilder(args);

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
    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    // Apply migrations
    AuthGate.Auth.MigrationManager.ApplyMigrations(app);

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
