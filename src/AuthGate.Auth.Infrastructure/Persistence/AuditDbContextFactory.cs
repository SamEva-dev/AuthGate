using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthGate.Auth.Infrastructure.Persistence;

public class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=AuthGate_Audit;Username=postgres;Password=locaguest");
        return new AuditDbContext(optionsBuilder.Options);
    }
}
