using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user with email and password
/// </summary>
public record LoginCommand : IRequest<Result<LoginResponseDto>>, IAuditableCommand
{
    /// <summary>
    /// User email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User password
    /// </summary>
    public required string Password { get; init; }

    public AuditAction AuditAction => AuditAction.Login;

    public string GetAuditDescription() => $"User login attempt for email: {Email}";
}
