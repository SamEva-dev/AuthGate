
using MediatR;

namespace AuthGate.Auth.Application.Features.Password;

public record ForgotPasswordCommand(string Email): IRequest
{
    private string Ip { get; set; }
    private string UserAgent { get; set; }

    public void SetIp(string ip) => Ip = ip;
    public void SetUserAgent(string ua) => UserAgent = ua;

    public string GetUserAgent() => UserAgent;
    public string GetIp() => Ip;
}
