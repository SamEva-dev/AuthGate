using AuthGate.Auth.Application.Common.Behaviors;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AuthGate.Auth.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Add pipeline behaviors
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        // FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // AutoMapper - manual configuration (profiles will be added later)
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(assembly);
        });
        services.AddSingleton(mapperConfig);
        services.AddScoped<IMapper>(sp => new Mapper(mapperConfig, sp.GetService));

        return services;
    }
}