using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Provides tenant context information extracted from JWT claims
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id");
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                return tenantId;
            }
            return null;
        }
    }

    public string? TenantCode => _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_code")?.Value;

    public string? TenantName => _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_name")?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsSuperAdmin
    {
        get
        {
            var roles = _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)
                .Select(c => c.Value) ?? Enumerable.Empty<string>();
            
            return roles.Contains(Roles.SuperAdmin);
        }
    }
}
