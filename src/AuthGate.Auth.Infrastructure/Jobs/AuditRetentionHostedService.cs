using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthGate.Auth.Infrastructure.Jobs;

public class AuditRetentionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<AuditRetentionOptions> _options;
    private readonly ILogger<AuditRetentionHostedService> _logger;

    public AuditRetentionHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<AuditRetentionOptions> options,
        ILogger<AuditRetentionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = _options.CurrentValue;
            var retentionDays = opts.Days <= 0 ? 7 : opts.Days;
            var intervalHours = opts.RunIntervalHours <= 0 ? 24 : opts.RunIntervalHours;

            try
            {
                await PurgeAsync(retentionDays, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing audit retention job");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PurgeAsync(int retentionDays, CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        using var scope = _scopeFactory.CreateScope();
        var auditDbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        _logger.LogInformation("Starting audit retention purge (retention: {RetentionDays} days, cutoff: {CutoffDate})", retentionDays, cutoffDate);

        var deletedAuditLogs = await auditDbContext.Database.ExecuteSqlRawAsync(
            @"DELETE FROM ""AuditLogs"" WHERE ""CreatedAtUtc"" < {0}",
            new object[] { cutoffDate },
            cancellationToken);

        _logger.LogInformation(
            "Audit retention purge completed. Deleted {DeletedAuditLogs} AuditLogs",
            deletedAuditLogs);
    }
}
