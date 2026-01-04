using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.AcceptLocaGuestInvitation;

public sealed record AcceptLocaGuestInvitationCommand : IRequest<Result<AcceptLocaGuestInvitationResponse>>
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}

public sealed record AcceptLocaGuestInvitationResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
