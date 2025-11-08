using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthGate.Auth.Infrastructure.Persistence.Factories
{
    public class AuditSqliteDesignTimeFactory : IDesignTimeDbContextFactory<AuditDbContext>
    {
        public AuditDbContext CreateDbContext(string[] args)
        {
            var connectionString = "Data Source=./Data/AuthGateAudit.db";

            var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
            optionsBuilder.UseSqlite(connectionString,
                b => b.MigrationsAssembly(typeof(AuditDbContext).Assembly.FullName));

            return new AuditDbContext(optionsBuilder.Options);
        }
    }
}
