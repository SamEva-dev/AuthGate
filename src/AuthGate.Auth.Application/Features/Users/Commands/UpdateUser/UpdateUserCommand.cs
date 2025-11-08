using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command to update user information
/// </summary>
public record UpdateUserCommand : IRequest<Result<bool>>, IAuditableCommand
{
    public Guid UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? IsActive { get; init; }

    public AuditAction AuditAction => AuditAction.UserUpdated;
    public string GetAuditDescription() => $"Updated user {UserId}";
}
