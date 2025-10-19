using Microsoft.AspNetCore.Authorization;

namespace AuthGate.Auth.Presentation.Security;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
        : base($"{nameof(HasPermissionAttribute)}:{permission}") { }
}