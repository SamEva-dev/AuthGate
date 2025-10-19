using Microsoft.AspNetCore.Authorization;

namespace AuthGate.Auth.Presentation.Security;

public class HasPermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }
    public HasPermissionRequirement(string code) => PermissionCode = code;
}