using AuthGate.Auth.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace AuthGate.Auth.Presentation.Security;

#nullable enable

/// <summary>
/// Loads authorization policies dynamically from the Permissions table.
/// </summary>
public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly ILogger<DynamicPermissionPolicyProvider> _logger;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly IServiceScopeFactory _scopeFactory;

    public DynamicPermissionPolicyProvider(IConfiguration config, IServiceScopeFactory scopeFactory, ILogger<DynamicPermissionPolicyProvider> logger)
    {
        var options = new AuthorizationOptions();
        _fallback = new DefaultAuthorizationPolicyProvider(Microsoft.Extensions.Options.Options.Create(options));
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task<AuthorizationPolicy?> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Ex: "HasPermission:CanDeleteUser"
        if (!policyName.StartsWith("HasPermission:"))
            return await _fallback.GetPolicyAsync(policyName);
        if (_cache.TryGetValue(policyName, out AuthorizationPolicy? cached))
            return cached;

        var permissionCode = policyName.Split(':', 2)[1];
        _logger.LogDebug("🔍 Checking dynamic policy for permission: {PermissionCode}", permissionCode);

        // Resolve a scoped IUnitOfWork for the DB access
        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var permission = (await uow.Permissions.GetAllAsync())
            .FirstOrDefault(p => p.Code.Equals(permissionCode, StringComparison.OrdinalIgnoreCase));

        if (permission is null)
        {
            _logger.LogWarning("⚠️ Permission {PermissionCode} not found in database.", permissionCode);
            // fall back to the default provider when permission not found
            return await _fallback.GetPolicyAsync(policyName);
        }

        _logger.LogInformation("✅ Dynamic policy loaded for permission {PermissionCode}", permissionCode);

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new HasPermissionRequirement(permission.Code))
            .Build();
        _cache.Set(policyName, policy, TimeSpan.FromMinutes(10)); // cache 10 min
        return policy;
    }
}