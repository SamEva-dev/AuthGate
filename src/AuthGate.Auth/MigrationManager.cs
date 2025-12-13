using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AuthGate.Auth;

public static class MigrationManager
{
    public static IHost ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        
        try
        {
            // Apply AuthDbContext migrations
            var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            Log.Information("Applying AuthGate database migrations...");
                authDb.Database.Migrate();
            Log.Information("✅ AuthGate database migrated successfully.");
            
            // Apply AuditDbContext migrations
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            Log.Information("Applying Audit database migrations...");
                auditDb.Database.Migrate();
            Log.Information("✅ Audit database migrated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Migration failed.");
            throw;
        }
        
        return host;
    }
    
    public static async Task<IHost> ApplyMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        
        try
        {
            // Apply AuthDbContext migrations
            var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            Log.Information("Applying AuthGate database migrations...");
            await authDb.Database.MigrateAsync();
            Log.Information("✅ AuthGate database migrated successfully.");
            
            // Apply AuditDbContext migrations
            var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            Log.Information("Applying Audit database migrations...");
            await auditDb.Database.MigrateAsync();
            Log.Information("✅ Audit database migrated successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Migration failed.");
            throw;
        }
        
        return host;
    }
}
