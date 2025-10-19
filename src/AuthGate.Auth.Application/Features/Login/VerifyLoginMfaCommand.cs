
using AuthGate.Auth.Application.DTOs;
using MediatR;

namespace AuthGate.Auth.Application.Features.Login;

public record VerifyLoginMfaCommand(Guid UserId, string Code) : IRequest<LoginResponse>
{
    private string Ip { get; set; }
    private string Agent { get; set; }
    public void SetIp(string ip) => Ip = ip;
    public string GetIp() => Ip;
    public void SetAgent(string agent) => Agent = agent;
    public string GetAgent() => Agent;
}
