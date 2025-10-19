using Serilog;
using System.Diagnostics;

namespace AuthGate.Auth.Presentation.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path;
        var method = context.Request.Method;

        Log.Information("➡️ {Method} {Path} started", method, path);

        try
        {
            await _next(context);
            stopwatch.Stop();
            Log.Information("✅ {Method} {Path} completed in {Elapsed} ms (Status {StatusCode})",
                method, path, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log.Error(ex, "❌ {Method} {Path} failed after {Elapsed} ms", method, path, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            Log.Debug("🏁 {Method} {Path} request finished", method, path);
        }
    }
}