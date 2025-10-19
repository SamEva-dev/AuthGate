
using AuthGate.Auth.Application.DTOs;
using MediatR;

namespace AuthGate.Auth.Application.Features.Register;

public record RegisterCommand(string FullName, string Email, string Password) : IRequest<LoginResponse>
{
    private string Ip { get; set; }
    private string UserAgent { get; set; }

    public void SetIp(string ip) => Ip = ip;
    public void SetUserAgent(string ua) => UserAgent = ua;

    public string GetUserAgent() => UserAgent;
    public string GetIp() => Ip;
}