using Serilog;
using SerilogTracing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using AuthGate.Auth;
using AuthGate.Auth.Infrastructure;
using AuthGate.Auth.Application;
using AuthGate.Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using AuthGate.Auth.Infrastructure.Persistence;
using AuthGate.Auth.Infrastructure;

var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", false, true);

IConfiguration configuration = configurationBuilder.Build();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.Console()
  // .WriteTo.File("logs/authgate-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.SQLite(
        sqliteDbPath: $"Data Source={configuration.GetConnectionString("Audit")};",
        tableName: "Logs",
        storeTimestampInUtc: true)
     .Enrich.WithEnvironmentUserName()
     .Enrich.WithClientIp()
    .ReadFrom.Configuration(configuration)
    .Filter.ByExcluding(evt =>
        evt.Properties.ContainsKey("transcriptPath") ||
        evt.Properties.ContainsKey("audioPath") ||
        evt.Properties.ContainsKey("summaryPath")
    )
    .CreateLogger();

using var listener = new ActivityListenerConfiguration()
    .Instrument.AspNetCoreRequests()
    .TraceToSharedLogger();
if (!Directory.Exists("Data")) Directory.CreateDirectory("Data");


Log.Information("*** STARTUP ***");
var requiredPythonLibs = new[] { "torch",
                                "faster-whisper",
                                "pyannote.audio",
                                "sentencepiece",
                                "python-multipart",
                                "pydantic",
                                "langdetect"};



var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton(configuration!);
                if (configuration == null)
                {
                    throw new InvalidOperationException("Configuration not initialized");
                }
                services.AddApplication();
                services.AddInfrastructure(configuration);
                services
                    .AddIdentityCore<User>(options =>
                    {
                        // Password settings
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireNonAlphanumeric = true;
                        options.Password.RequiredLength = 8;

                        // Lockout settings
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                        options.Lockout.MaxFailedAccessAttempts = 5;
                        options.Lockout.AllowedForNewUsers = true;

                        // User settings
                        options.User.RequireUniqueEmail = true;

                        // SignIn settings
                        options.SignIn.RequireConfirmedEmail = false;
                        options.SignIn.RequireConfirmedAccount = false;
                    })
                    .AddRoles<Role>() // ? Ajoute la gestion des rôles ici
                    .AddEntityFrameworkStores<AuthDbContext>() // ? ton DbContext
                    .AddDefaultTokenProviders().AddSignInManager();
            })
            .ConfigureLogging(logger =>
            {
                logger.ClearProviders();
                logger.AddConsole();
                logger.AddSerilog(dispose: true);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(options =>
                {
                    options.ListenAnyIP(8081, listenOptions =>
                    {
                        listenOptions.UseHttps();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    options.ListenAnyIP(8080, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });
                });
                webBuilder.UseStartup<Startup>();
            })
            .UseSerilog()
            .Build();

host.ApplyMigrations();

host.Run();