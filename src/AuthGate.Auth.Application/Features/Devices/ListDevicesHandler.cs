using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Features.Register;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Domain.Entities;
using MediatR;

namespace AuthGate.Auth.Application.Features.Devices;

public class ListDevicesHandler : IRequestHandler<ListDevicesQuery, IEnumerable<DeviceSessionDto>>
{
    private readonly IUnitOfWork _uow;
    public ListDevicesHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<IEnumerable<DeviceSessionDto>> Handle(ListDevicesQuery request, CancellationToken cancellationToken)
    {
        var list = await _uow.Auth.ListSessionsAsync(request.userId);
        return list.Select(s => new DeviceSessionDto(s.Id, s.IpAddress, s.UserAgent, s.CreatedAtUtc, s.ExpiresAtUtc, s.IsActive));
    }
}