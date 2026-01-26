using AuthGate.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;

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

            options.AddPolicy("ManagerAppRequired", policy =>
                policy.RequireAssertion(context =>
                {
                    var app = context.User?.FindFirst("app")?.Value;
                    return string.Equals(app, "manager", StringComparison.OrdinalIgnoreCase);
                }));

            options.AddPolicy("NoPasswordChangeRequired", policy =>
                policy.RequireAssertion(context =>
                {
                    var claim = context.User?.FindFirst("pwd_change_required");
                    return claim == null || !string.Equals(claim.Value, "true", StringComparison.OrdinalIgnoreCase);
                }));

            options.AddPolicy("TenantContextRequired", policy =>
                policy.RequireAssertion(context =>
                {
                    var pwd = context.User?.FindFirst("pwd_change_required");
                    if (pwd != null && string.Equals(pwd.Value, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    var orgIdValue = context.User?.FindFirst("org_id")?.Value
                        ?? context.User?.FindFirst("organization_id")?.Value;

                    return !string.IsNullOrWhiteSpace(orgIdValue)
                        && Guid.TryParse(orgIdValue, out var orgId)
                        && orgId != Guid.Empty;
                }));
        });

        return services;
    }
}
