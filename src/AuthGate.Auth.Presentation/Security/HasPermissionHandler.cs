using AuthGate.Auth.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthGate.Auth.Presentation.Security;

/// <summary>
/// Handles permission checks dynamically using the database.
/// </summary>
public class HasPermissionHandler : AuthorizationHandler<HasPermissionRequirement>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<HasPermissionHandler> _logger;

    public HasPermissionHandler(IUnitOfWork uow, ILogger<HasPermissionHandler> logger)
    {
        _uow = uow;
        _logger = logger;
    }


    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, HasPermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (userId is null)
        {
            _logger.LogWarning("Permission check failed: no user claim present.");
            context.Fail();
            return;
        }

        // Vérifie si l'utilisateur a un rôle possédant la permission
        var userGuid = Guid.Parse(userId);
        var user = await _uow.Users.GetByIdWithRolesAndPermissionsAsync(userGuid);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Permission check failed: user {UserId} not found.", userId);
            context.Fail();
            return;
        }

        var hasPermission = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Any(rp => rp.Permission.Code == requirement.PermissionCode);

        if (hasPermission)
        {
            _logger.LogInformation("✅ User {UserId} granted permission {Permission}", userId, requirement.PermissionCode);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("❌ User {UserId} missing permission {Permission}", userId, requirement.PermissionCode);
            context.Fail();
        }
    }
}