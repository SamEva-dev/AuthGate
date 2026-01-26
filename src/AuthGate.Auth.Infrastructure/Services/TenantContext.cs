using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AuthGate.Auth.Infrastructure.Services;

/// <summary>
/// Provides tenant context information extracted from JWT claims
/// </summary>
public class TenantContext : IOrganizationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OrganizationId
    {
        get
        {
            // Prefer org_id (token context), fallback to organization_id and legacy tenant_id
            var organizationIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimNames.OrgId)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimNames.OrganizationId)?.Value;
            if (!string.IsNullOrEmpty(organizationIdClaim) && Guid.TryParse(organizationIdClaim, out var orgId))
            {
                return orgId;
            }

            var legacyTenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(legacyTenantIdClaim) && Guid.TryParse(legacyTenantIdClaim, out var tenantId))
            {
                return tenantId;
            }

            return null;
        }
    }

    public string? OrganizationCode =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("organization_code")?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_code")?.Value;

    public string? OrganizationName =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("organization_name")?.Value
        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_name")?.Value;

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
