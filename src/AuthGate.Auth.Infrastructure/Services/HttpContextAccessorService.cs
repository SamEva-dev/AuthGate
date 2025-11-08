using AuthGate.Auth.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AuthGate.Auth.Infrastructure.Services;

public class HttpContextAccessorService : Application.Common.Interfaces.IHttpContextAccessor
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public HttpContextAccessorService(Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        // Check for forwarded IP first (for reverse proxies)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    public string? GetUserAgent()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Request.Headers["User-Agent"].FirstOrDefault();
    }

    public Guid? GetCurrentUserId()
    {
        var context = _httpContextAccessor.HttpContext;
        var userIdClaim = context?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context?.User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}
