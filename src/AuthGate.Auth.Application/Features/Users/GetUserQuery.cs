using AuthGate.Auth.Application.DTOs;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users;

public record GetUserQuery(Guid Id) :IRequest<UserDto?>
{
    private Guid? UserId { get; set; }
    private string? Ip { get; set; }
    private string? Agent { get; set; }

    public void SetUserInfo(Guid userId, string ip, string agent)
    {
        UserId = userId;
        Ip = ip;
        Agent = agent;
    }
    public string? GetIP() => Ip;
    public Guid? GetUserId() => UserId;
    public string? GetAgent() => Agent;
}
