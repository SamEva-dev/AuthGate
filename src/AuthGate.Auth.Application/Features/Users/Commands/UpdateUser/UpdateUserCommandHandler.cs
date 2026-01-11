using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Services;
using DomainRoles = AuthGate.Auth.Domain.Constants.Roles;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Handler for UpdateUserCommand
/// </summary>
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationContext _organizationContext;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        UserManager<User> userManager,
        ICurrentUserService currentUserService,
        IOrganizationContext organizationContext,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _organizationContext = organizationContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            _logger.LogWarning("User not found for update: {UserId}", request.UserId);
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
                _logger.LogWarning("Cross-tenant user update blocked. TargetUserId={TargetUserId}", request.UserId);
                return Result.Failure<bool>("User not found");
            }
        }

        if (request.IsActive.HasValue && request.IsActive.Value == false)
        {
            var targetRoles = await _userManager.GetRolesAsync(user);
            var isTenantOwner = targetRoles.Contains(DomainRoles.TenantOwner);
            if (isTenantOwner && user.OrganizationId.HasValue)
            {
                var owners = await _userManager.GetUsersInRoleAsync(DomainRoles.TenantOwner);
                var activeOwnersInOrg = owners.Count(u => u.IsActive && u.OrganizationId == user.OrganizationId);
                if (user.IsActive && activeOwnersInOrg <= 1)
                {
                    _logger.LogWarning("Blocked disabling last TenantOwner. TargetUserId={TargetUserId}", request.UserId);
                    return Result.Failure<bool>("Cannot deactivate the last TenantOwner of the organization");
                }
            }
        }

        // Update properties if provided
        if (request.FirstName != null)
            user.FirstName = request.FirstName;

        if (request.LastName != null)
            user.LastName = request.LastName;

        if (request.PhoneNumber != null)
            user.PhoneNumber = request.PhoneNumber;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update user {UserId}: {Errors}", request.UserId, errors);
            return Result.Failure<bool>($"Update failed: {errors}");
        }

        _logger.LogInformation("User updated successfully: {UserId}", request.UserId);
        return Result.Success(true);
    }
}
