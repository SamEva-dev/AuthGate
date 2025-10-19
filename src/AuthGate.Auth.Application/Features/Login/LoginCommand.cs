using AuthGate.Auth.Application.DTOs;
using MediatR;


namespace AuthGate.Auth.Application.Features.Login;

public sealed record LoginCommand(string Email, string Password, string? Code = null) : IRequest<LoginResponse>
{
    private string Ip { get; set; }
    private string UserAgent { get; set; }

    public void SetIp(string ip) => Ip = ip;
    public void SetUserAgent(string ua) => UserAgent = ua;

    public string GetUserAgent() => UserAgent;
    public string GetIp() => Ip;
}
