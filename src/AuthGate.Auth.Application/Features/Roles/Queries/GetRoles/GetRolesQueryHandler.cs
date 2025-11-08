using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Roles;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Application.Features.Roles.Queries.GetRoles;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, Result<List<RoleDto>>>
{
    private readonly RoleManager<Role> _roleManager;
    private readonly DbContext _context;

    public GetRolesQueryHandler(RoleManager<Role> roleManager, DbContext context)
    {
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<Result<List<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _roleManager.Roles.ToListAsync(cancellationToken);
        var roleDtos = new List<RoleDto>();

        foreach (var role in roles)
        {
            var userCount = await _context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>()
                .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

            var permissionCount = await _context.Set<RolePermission>()
                .CountAsync(rp => rp.RoleId == role.Id, cancellationToken);

            roleDtos.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                IsSystemRole = role.IsSystemRole,
                CreatedAtUtc = role.CreatedAtUtc,
                UserCount = userCount,
                PermissionCount = permissionCount
            });
        }

        return Result.Success(roleDtos);
    }
}
