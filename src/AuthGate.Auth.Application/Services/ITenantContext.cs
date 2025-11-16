namespace AuthGate.Auth.Application.Services;

/// <summary>
/// Provides tenant context information for the current request
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the current tenant code (T0001, T0002...)
    /// </summary>
    string? TenantCode { get; }

    /// <summary>
    /// Gets the current tenant name
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    /// Checks if the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the current user is a SuperAdmin (can access all tenants)
    /// </summary>
    bool IsSuperAdmin { get; }
}
