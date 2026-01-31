using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace AuthGate.Auth.Infrastructure.Jobs;

public class UnconfirmedAccountCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<UnconfirmedAccountCleanupOptions> _options;
    private readonly ILogger<UnconfirmedAccountCleanupHostedService> _logger;

    public UnconfirmedAccountCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IOptionsMonitor<UnconfirmedAccountCleanupOptions> options,
        ILogger<UnconfirmedAccountCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            var ttlMinutes = _configuration.GetValue<int?>("Auth:UnconfirmedAccountTtlMinutes")
                ?? ((_configuration.GetValue<int?>("Auth:UnconfirmedAccountTtlHours") ?? 0) * 60);

            var opts = _options.CurrentValue;
            var intervalMinutes = opts.RunIntervalMinutes <= 0 ? 5 : opts.RunIntervalMinutes;
            var batchSize = opts.BatchSize <= 0 ? 100 : opts.BatchSize;

            if (ttlMinutes > 0)
            {
                try
                {
                    await PurgeAsync(ttlMinutes, batchSize, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing unconfirmed account cleanup job");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PurgeAsync(int ttlMinutes, int batchSize, CancellationToken cancellationToken)
    {
        var cutoffUtc = DateTime.UtcNow.AddMinutes(-ttlMinutes);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var users = await db.Users
            .Where(u => !u.EmailConfirmed && u.Status == UserStatus.PendingEmailConfirmation && u.CreatedAtUtc < cutoffUtc)
            .OrderBy(u => u.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return;

        _logger.LogInformation(
            "Starting unconfirmed account cleanup (ttlMinutes: {TtlMinutes}, cutoffUtc: {CutoffUtc}, batchSize: {BatchSize}). Candidates: {Candidates}",
            ttlMinutes,
            cutoffUtc,
            batchSize,
            users.Count);

        var deleted = 0;
        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning(
                    "Failed to delete expired unconfirmed user {UserId} ({Email}): {Errors}",
                    user.Id,
                    user.Email,
                    errors);
                continue;
            }

            deleted++;
        }

        _logger.LogInformation(
            "Unconfirmed account cleanup completed. Deleted: {Deleted} (candidates: {Candidates})",
            deleted,
            users.Count);
    }
}
