using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Features.Mfa;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users;

public class ListUsersHandler : IRequestHandler<ListUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<DisableMfaHandler> _logger;

    public ListUsersHandler(IUnitOfWork uow, IAuditService audit, ILogger<DisableMfaHandler> logger)
    {
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    //await handler.Handle(userId, ip, agent);
    public async Task<IEnumerable<UserDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _uow.Auth.GetAllAsync();
        var result = users.Select(u =>
            new UserDto(u.Id, u.Email, u.FullName, u.MfaEnabled, u.IsLocked, u.IsDeleted));

        _logger.LogInformation("👁️ User list viewed by {UserId} ({Ip})", request.UserId, request.Ip);
        await _audit.LogAsync("UsersListViewed", "User list fetched", request.UserId.ToString(), null, request.Ip, request.Agent);

        return result;
    }
}