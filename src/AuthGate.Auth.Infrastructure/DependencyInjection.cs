using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Infrastructure.Identity;
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

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IMfaService, MfaService>();
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}
