using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthGate.Auth.Infrastructure.Persistence.Factories
{
    public class AuthSqliteDesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext(string[] args)
        {
            var connectionString = "Data Source=./Data/AuthGate.db";

            var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
            optionsBuilder.UseSqlite(connectionString,
                b => b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));

            return new AuthDbContext(optionsBuilder.Options);
        }
    }
}
