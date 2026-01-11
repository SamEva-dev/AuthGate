using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Services;
using DomainRoles = AuthGate.Auth.Domain.Constants.Roles;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Handler for DeleteUserCommand (soft delete)
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationContext _organizationContext;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        UserManager<User> userManager,
        ICurrentUserService currentUserService,
        IOrganizationContext organizationContext,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _organizationContext = organizationContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            _logger.LogWarning("User not found for deletion: {UserId}", request.UserId);
            return Result.Failure<bool>("User not found");
        }

        var isSuperAdmin = _currentUserService.Roles.Contains(DomainRoles.SuperAdmin);
        if (!isSuperAdmin)
        {
            if (!_organizationContext.OrganizationId.HasValue)
            {
                return Result.Failure<bool>("Tenant context not found");
            }

            var orgId = _organizationContext.OrganizationId.Value;
            if (user.OrganizationId != orgId)
            {
                _logger.LogWarning("Cross-tenant user delete blocked. TargetUserId={TargetUserId}", request.UserId);
                return Result.Failure<bool>("User not found");
            }
        }

        var targetRoles = await _userManager.GetRolesAsync(user);
        var isTenantOwner = targetRoles.Contains(DomainRoles.TenantOwner);
        if (isTenantOwner && user.OrganizationId.HasValue)
        {
            var owners = await _userManager.GetUsersInRoleAsync(DomainRoles.TenantOwner);
            var activeOwnersInOrg = owners.Count(u => u.IsActive && u.OrganizationId == user.OrganizationId);
            if (user.IsActive && activeOwnersInOrg <= 1)
            {
                _logger.LogWarning("Blocked deactivation of last TenantOwner. TargetUserId={TargetUserId}", request.UserId);
                return Result.Failure<bool>("Cannot deactivate the last TenantOwner of the organization");
            }
        }

        // Soft delete: set IsActive to false
        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to delete user {UserId}: {Errors}", request.UserId, errors);
            return Result.Failure<bool>($"Delete failed: {errors}");
        }

        _logger.LogInformation("User soft deleted: {UserId}", request.UserId);
        return Result.Success(true);
    }
}
