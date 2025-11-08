using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Roles.Commands.AssignPermissionToRole;

public class AssignPermissionToRoleCommandHandler : IRequestHandler<AssignPermissionToRoleCommand, Result<bool>>
{
    private readonly DbContext _context;
    private readonly ILogger<AssignPermissionToRoleCommandHandler> _logger;

    public AssignPermissionToRoleCommandHandler(DbContext context, ILogger<AssignPermissionToRoleCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(AssignPermissionToRoleCommand request, CancellationToken cancellationToken)
    {
        // Check if already assigned
        var exists = await _context.Set<RolePermission>()
            .AnyAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId, cancellationToken);

        if (exists)
        {
            return Result.Failure<bool>("Permission already assigned to this role");
        }

        var rolePermission = new RolePermission
        {
            RoleId = request.RoleId,
            PermissionId = request.PermissionId
        };

        _context.Set<RolePermission>().Add(rolePermission);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Permission {PermissionId} assigned to role {RoleId}", request.PermissionId, request.RoleId);
        return Result.Success(true);
    }
}
