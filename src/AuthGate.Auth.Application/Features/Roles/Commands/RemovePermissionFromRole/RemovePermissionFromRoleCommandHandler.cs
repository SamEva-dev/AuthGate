using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Roles.Commands.RemovePermissionFromRole;

public class RemovePermissionFromRoleCommandHandler : IRequestHandler<RemovePermissionFromRoleCommand, Result<bool>>
{
    private readonly DbContext _context;
    private readonly ILogger<RemovePermissionFromRoleCommandHandler> _logger;

    public RemovePermissionFromRoleCommandHandler(DbContext context, ILogger<RemovePermissionFromRoleCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RemovePermissionFromRoleCommand request, CancellationToken cancellationToken)
    {
        var rolePermission = await _context.Set<RolePermission>()
            .FirstOrDefaultAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId, cancellationToken);

        if (rolePermission == null)
        {
            return Result.Failure<bool>("Permission not assigned to this role");
        }

        _context.Set<RolePermission>().Remove(rolePermission);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Permission {PermissionId} removed from role {RoleId}", request.PermissionId, request.RoleId);
        return Result.Success(true);
    }
}
