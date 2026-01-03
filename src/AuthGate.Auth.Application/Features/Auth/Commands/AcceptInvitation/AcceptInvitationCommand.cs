using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.AcceptInvitation;

/// <summary>
/// Command to accept an invitation and create a user account
/// The user will be automatically linked to the inviter's organization
/// </summary>
public record AcceptInvitationCommand : IRequest<Result<AcceptInvitationResponse>>
{
    /// <summary>
    /// Invitation token from URL
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; init; } = string.Empty;
}

public record AcceptInvitationResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string OrganizationCode { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
