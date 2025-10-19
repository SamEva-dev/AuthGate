using AuthGate.Auth.Application.DTOs;
using MediatR;

namespace AuthGate.Auth.Application.Features.Devices;

public record ListDevicesQuery(Guid userId) : IRequest<IEnumerable<DeviceSessionDto>>
{
}
