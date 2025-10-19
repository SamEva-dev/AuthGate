using MediatR;

namespace AuthGate.Auth.Application.Features.Users;

public record DeleteUserCommand(Guid userId) : IRequest
{
    private string Ip { get; set; }
    private string PerformedBy { get; set; }

    public void SetIp(string ip) => Ip = ip;    
    public string GetIp() => Ip;
    public void SetPerformedBy(string performedBy) => PerformedBy = performedBy;
    public string GetPerformedBy() => PerformedBy;
}
