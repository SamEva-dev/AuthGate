using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AuthGate.Auth
{
    public static class MigrationManager
    {
        public static IHost ApplyMigrations(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbContext>();
            try
            {
                db.Database.Migrate();
                Log.Information("✅ Database migrated successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ Migration failed.");
            }
            return host;
        }
    }
}
