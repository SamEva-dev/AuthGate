using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Common.Models;
using AuthGate.Auth.Application.DTOs.Users;
using AuthGate.Auth.Application.Services;
using DomainRoles = AuthGate.Auth.Domain.Constants.Roles;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Handler for GetUsersQuery
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserDto>>>
{
    private readonly UserManager<User> _userManager;
    private readonly IUserRoleService _userRoleService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationContext _organizationContext;
    private readonly ILogger<GetUsersQueryHandler> _logger;

    public GetUsersQueryHandler(
        UserManager<User> userManager,
        IUserRoleService userRoleService,
        ICurrentUserService currentUserService,
        IOrganizationContext organizationContext,
        ILogger<GetUsersQueryHandler> logger)
    {
        _userManager = userManager;
        _userRoleService = userRoleService;
        _currentUserService = currentUserService;
        _organizationContext = organizationContext;
        _logger = logger;
    }

    public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _userManager.Users.AsQueryable();

        var isSuperAdmin = _currentUserService.Roles.Contains(DomainRoles.SuperAdmin);
        if (!isSuperAdmin)
        {
            if (!_organizationContext.OrganizationId.HasValue)
            {
                return Result.Failure<PagedResult<UserDto>>("Tenant context not found");
            }

            var orgId = _organizationContext.OrganizationId.Value;
            query = query.Where(u => u.OrganizationId == orgId);
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userRoleService.GetUserRolesAsync(user);
            
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                MfaEnabled = user.MfaEnabled,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAtUtc = user.CreatedAtUtc,
                LastLoginAtUtc = user.LastLoginAtUtc,
                Roles = roles.ToList()
            });
        }

        var result = new PagedResult<UserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Result.Success(result);
    }
}
