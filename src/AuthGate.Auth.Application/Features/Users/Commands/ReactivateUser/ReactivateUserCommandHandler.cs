using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Services;
using DomainRoles = AuthGate.Auth.Domain.Constants.Roles;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Commands.ReactivateUser;

public class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationContext _organizationContext;
    private readonly ILogger<ReactivateUserCommandHandler> _logger;

    public ReactivateUserCommandHandler(
        UserManager<User> userManager,
        ICurrentUserService currentUserService,
        IOrganizationContext organizationContext,
        ILogger<ReactivateUserCommandHandler> logger)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
        _organizationContext = organizationContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found for reactivation: {UserId}", request.UserId);
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
                _logger.LogWarning("Cross-tenant user reactivate blocked. TargetUserId={TargetUserId}", request.UserId);
                return Result.Failure<bool>("User not found");
            }
        }

        user.IsActive = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to reactivate user {UserId}: {Errors}", request.UserId, errors);
            return Result.Failure<bool>($"Reactivate failed: {errors}");
        }

        _logger.LogInformation("User reactivated: {UserId}", request.UserId);
        return Result.Success(true);
    }
}
