using Microsoft.AspNetCore.Authorization;

namespace AuthGate.Auth.Authorization;

/// <summary>
/// Attribute to require specific permission for endpoint access
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Creates a new HasPermission attribute
    /// </summary>
    /// <param name="permission">Required permission code (e.g., "users.read")</param>
    public HasPermissionAttribute(string permission)
        : base(policy: $"Permission:{permission}")
    {
    }
}
