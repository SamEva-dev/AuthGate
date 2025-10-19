using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using MediatR;

namespace AuthGate.Auth.Application.Features.Devices;

public class RevokeOneDeviceHandler : IRequestHandler<RevokeOneDeviceCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public RevokeOneDeviceHandler(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow; 
        _audit = audit;
    }

    public async Task Handle(RevokeOneDeviceCommand request, CancellationToken cancellationToken)
    {
        var sessions = await _uow.Auth.ListSessionsAsync(request.GetUserId());
        var s = sessions.FirstOrDefault(x => x.Id == request.DeviceId);
        if (s is null || s.IsRevoked) return;

        await _uow.Auth.RevokeSessionAsync(s);
        await _uow.SaveChangesAsync();
        await _audit.LogAsync("DeviceRevoked", $"Device {request.DeviceId} revoked", request.GetUserId().ToString(), null, request.GetIp(), null);
    }
}