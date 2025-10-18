using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthGate.Auth.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            var dbPath = config.GetConnectionString("Sqlite") ?? "Data/meetmind.db";
            services.AddDbContext<AuthGateDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            return services;
        }
    }
}
