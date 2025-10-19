
using MediatR;

namespace AuthGate.Auth.Application.Features.Mfa;

public record DisableMfaCommand(Guid UserId): IRequest
{
    private Guid UserId { get; set; }
    public void SetUserId(Guid userId) => UserId = userId;
    public Guid GetUserId() => UserId;
}
