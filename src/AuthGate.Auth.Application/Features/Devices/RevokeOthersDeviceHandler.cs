
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using MediatR;
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Features.Devices;

public class RevokeDeviceHandler : IRequestHandler<RevokeOthersDeviceCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;

    public RevokeDeviceHandler(IUnitOfWork uow, IAuditService audit)
    {
        _uow = uow; _audit = audit;
    }


    public async Task Handle(RevokeOthersDeviceCommand request, CancellationToken cancellationToken)
    {
        await _uow.Auth.RevokeAllOtherSessionsAsync(request.GetUserId(), request.CurrentId);
        await _uow.SaveChangesAsync();
        await _audit.LogAsync("DevicesRevoked", "All other devices revoked", request.GetUserId().ToString(), null, request.GetIp(), null);
    }
}