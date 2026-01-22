using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ValidateEmail;

public record ValidateEmailCommand : IRequest<Result<bool>>, IAuditableCommand
{
    public required string Email { get; init; }

    public required string Token { get; init; }

    public AuditAction AuditAction => AuditAction.UserUpdated;

    public string GetAuditDescription() => $"Email validation for {Email}";
}
