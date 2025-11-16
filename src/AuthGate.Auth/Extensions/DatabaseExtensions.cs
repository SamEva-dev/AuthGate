using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Apply database migrations automatically on application startup
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        
        try
        {
            // Apply AuthDbContext migrations
            var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            logger.LogInformation("Applying AuthGate database migrations...");
            await authContext.Database.MigrateAsync();
            logger.LogInformation("AuthGate database migrations applied successfully");
            
            // Apply AuditDbContext migrations
            var auditContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            logger.LogInformation("Applying Audit database migrations...");
            await auditContext.Database.MigrateAsync();
            logger.LogInformation("Audit database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying migrations");
            throw;
        }
    }
    
    /// <summary>
    /// Seed initial data (only in Development)
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<AuthGate.Auth.Infrastructure.Persistence.DataSeeding.AuthDbSeeder>();
            logger.LogInformation("Seeding AuthGate database...");
            await seeder.SeedAsync();
            logger.LogInformation("AuthGate database seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
