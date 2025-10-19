
using AuthGate.Auth.Application.DTOs;
using MediatR;

namespace AuthGate.Auth.Application.Features.Devices;

public record RevokeOthersDeviceCommand(Guid CurrentId) : IRequest
{
    private Guid UserId { get; set; }
    private string Ip { get; set; }
    public void SetUserId(Guid userId) => UserId = userId;
    public void SetIp(string ip) => Ip = ip;
    public Guid GetUserId() => UserId;
    public string GetIp() => Ip;
}
