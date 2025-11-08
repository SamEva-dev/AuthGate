using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace AuthGate.Auth.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting
/// </summary>
public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Policy for authentication endpoints (stricter)
            options.AddFixedWindowLimiter("auth", config =>
            {
                config.PermitLimit = 5; // 5 requests
                config.Window = TimeSpan.FromMinutes(1); // per minute
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 2;
            });

            // Policy for password reset (very strict)
            options.AddFixedWindowLimiter("password-reset", config =>
            {
                config.PermitLimit = 3; // 3 requests
                config.Window = TimeSpan.FromMinutes(15); // per 15 minutes
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });

            // Policy for registration (strict)
            options.AddFixedWindowLimiter("register", config =>
            {
                config.PermitLimit = 3; // 3 requests
                config.Window = TimeSpan.FromHours(1); // per hour
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });

            // General API policy (more permissive)
            options.AddFixedWindowLimiter("api", config =>
            {
                config.PermitLimit = 100; // 100 requests
                config.Window = TimeSpan.FromMinutes(1); // per minute
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 5;
            });

            // Global fallback policy
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Partition by IP address
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            // Rejection status code
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Custom response when rate limit exceeded
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? retryAfterValue.TotalSeconds
                    : 60;

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = $"{retryAfter} seconds"
                }, cancellationToken);
            };
        });

        return services;
    }
}
