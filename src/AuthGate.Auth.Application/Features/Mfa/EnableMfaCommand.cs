using MediatR;

namespace AuthGate.Auth.Application.Features.Mfa;

public record EnableMfaCommand(string Issuer = "AuthGate") : IRequest<(string Secret, string QrCodeBase64)>
{
    private Guid UserId { get; set; }
    public void SetUserId(Guid userId) => UserId = userId;
    public Guid GetUserId() => UserId;
}
