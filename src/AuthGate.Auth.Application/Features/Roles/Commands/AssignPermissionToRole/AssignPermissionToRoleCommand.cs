using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Roles.Commands.AssignPermissionToRole;

public record AssignPermissionToRoleCommand(Guid RoleId, Guid PermissionId) : IRequest<Result<bool>>, IAuditableCommand
{
    public AuditAction AuditAction => AuditAction.PermissionAssigned;
    public string GetAuditDescription() => $"Assigned permission {PermissionId} to role {RoleId}";
}
