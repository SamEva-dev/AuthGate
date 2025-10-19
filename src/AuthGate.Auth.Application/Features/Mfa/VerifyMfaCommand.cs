
using MediatR;

namespace AuthGate.Auth.Application.Features.Mfa;

public record VerifyMfaCommand(string Code) : IRequest<bool>
{
    private Guid UserId { get; set; }
    public void SetUserId(Guid userId) => UserId = userId;
    public Guid GetUserId() => UserId;
}
