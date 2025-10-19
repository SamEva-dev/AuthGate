using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuthGate.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}