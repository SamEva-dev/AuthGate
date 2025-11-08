using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Roles.Commands.RemovePermissionFromRole;

public record RemovePermissionFromRoleCommand(Guid RoleId, Guid PermissionId) : IRequest<Result<bool>>, IAuditableCommand
{
    public AuditAction AuditAction => AuditAction.PermissionRemoved;
    public string GetAuditDescription() => $"Removed permission {PermissionId} from role {RoleId}";
}
