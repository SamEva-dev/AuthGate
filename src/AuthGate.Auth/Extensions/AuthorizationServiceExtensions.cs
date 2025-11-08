using AuthGate.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace AuthGate.Auth.Extensions;

/// <summary>
/// Extension methods for configuring authorization services
/// </summary>
public static class AuthorizationServiceExtensions
{
    /// <summary>
    /// Adds permission-based authorization to the service collection
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        // Register custom policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        
        // Register permission authorization handler
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Add authorization with default policies
        services.AddAuthorization(options =>
        {
            // Default policy requires authentication
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Fallback policy (for endpoints without [Authorize])
            options.FallbackPolicy = null; // Allow anonymous by default

            // Optional: Add some common named policies
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequireMfa", policy =>
                policy.RequireClaim("mfa_enabled", "true"));
        });

        return services;
    }
}
