using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Register;

/// <summary>
/// Command for user registration
/// </summary>
public record RegisterCommand : IRequest<Result<RegisterResponseDto>>
{
    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User's password (validated in UI)
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// User's first name
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// User's last name
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// User's phone number
    /// </summary>
    public string? PhoneNumber { get; init; }
}
