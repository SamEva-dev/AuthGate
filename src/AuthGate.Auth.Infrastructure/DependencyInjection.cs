using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Infrastructure.Identity;
using AuthGate.Auth.Infrastructure.Persistence;
using AuthGate.Auth.Infrastructure.Repositories;
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

            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAuditRepository, AuditRepository>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IMfaService, MfaService>();



            return services;
        }
    }
}
