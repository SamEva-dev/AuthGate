using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace AuthGate.Auth.Middleware;

public sealed class ObservabilityEnrichmentMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public ObservabilityEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? context.TraceIdentifier;

        var traceId = Activity.Current?.TraceId.ToString();
        var spanId = Activity.Current?.SpanId.ToString();

        var user = context.User;
        var organizationId = user?.FindFirstValue("org_id") ?? user?.FindFirstValue("organization_id") ?? user?.FindFirstValue("organizationId");
        var userId = user?.FindFirstValue("sub") ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var clientId = user?.FindFirstValue("azp") ?? user?.FindFirstValue("client_id");

        using (LogContext.PushProperty("correlation_id", correlationId))
        using (LogContext.PushProperty("trace_id", traceId))
        using (LogContext.PushProperty("span_id", spanId))
        using (LogContext.PushProperty("organization_id", organizationId))
        using (LogContext.PushProperty("user_id", userId))
        using (LogContext.PushProperty("client_id", clientId))
        using (LogContext.PushProperty("route", context.Request.Path.Value))
        using (LogContext.PushProperty("http_method", context.Request.Method))
        {
            await _next(context);
        }
    }
}

public static class ObservabilityEnrichmentMiddlewareExtensions
{
    public static IApplicationBuilder UseObservabilityEnrichment(this IApplicationBuilder app)
        => app.UseMiddleware<ObservabilityEnrichmentMiddleware>();
}
