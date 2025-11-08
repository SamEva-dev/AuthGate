using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Command to delete (soft delete) a user
/// </summary>
public record DeleteUserCommand(Guid UserId) : IRequest<Result<bool>>, IAuditableCommand
{
    public AuditAction AuditAction => AuditAction.UserDeleted;
    public string GetAuditDescription() => $"Deleted user {UserId}";
}
