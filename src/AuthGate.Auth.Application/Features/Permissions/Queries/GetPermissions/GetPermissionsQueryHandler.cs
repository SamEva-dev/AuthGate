using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Permissions;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Application.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, Result<List<PermissionDto>>>
{
    private readonly DbContext _context;

    public GetPermissionsQueryHandler(DbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await _context.Set<Permission>()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        var permissionDtos = permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Code = p.Code,
            DisplayName = p.DisplayName,
            Description = p.Description,
            Category = p.Category,
            IsActive = p.IsActive
        }).ToList();

        return Result.Success(permissionDtos);
    }
}
