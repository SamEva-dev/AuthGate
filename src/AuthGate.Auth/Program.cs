using Serilog;
using SerilogTracing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using AuthGate.Auth.Presentation;
using AuthGate.Auth;
using AuthGate.Auth.Infrastructure;
using AuthGate.Auth.Application;
using static Org.BouncyCastle.Math.EC.ECCurve;

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
                    options.ListenAnyIP(5001, listenOptions =>
                    {
                        listenOptions.UseHttps();
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    });

                    options.ListenAnyIP(5000, listenOptions =>
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