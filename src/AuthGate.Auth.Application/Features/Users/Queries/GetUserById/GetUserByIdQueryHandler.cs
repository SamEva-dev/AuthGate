using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Users;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Handler for GetUserByIdQuery
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        UserManager<User> userManager,
        IUserRoleService userRoleService,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _userManager = userManager;
        _userRoleService = userRoleService;
        _logger = logger;
    }

    public async Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", request.UserId);
            return Result.Failure<UserDetailDto>("User not found");
        }

        var roles = await _userRoleService.GetUserRolesAsync(user);
        var permissions = await _userRoleService.GetUserPermissionsAsync(user);

        var userDetail = new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            MfaEnabled = user.MfaEnabled,
            EmailConfirmed = user.EmailConfirmed,
            FailedLoginAttempts = user.FailedLoginAttempts,
            IsLockedOut = user.IsLockedOut,
            LockoutEndUtc = user.LockoutEndUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            Roles = roles.ToList(),
            Permissions = permissions.ToList()
        };

        return Result.Success(userDetail);
    }
}
