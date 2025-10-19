using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Application.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users;

public class GetUserHandler : IRequestHandler<GetUserQuery, UserDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<GetUserHandler> _logger;

    public GetUserHandler(IUnitOfWork uow, IAuditService audit, ILogger<GetUserHandler> logger)
    {
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task<UserDto?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _uow.Auth.GetByIdAsync(request.Id);
        if (user is null)
        {
            _logger.LogWarning("❌ User {TargetId} not found when accessed by {UserId}", request.Id, request.GetUserId());
            await _audit.LogAsync("UserViewFailed", "Attempted to view non-existing user", request.Id.ToString(), null, request.GetIP(), request.GetAgent());
            return null;
        }

        var dto = new UserDto(user.Id, user.Email, user.FullName, user.MfaEnabled, user.IsLocked, user.IsDeleted);
        _logger.LogInformation("👁️ User {TargetId} viewed by {UserId}", request.Id, request.GetUserId());
        await _audit.LogAsync("UserViewed", $"User {request.Id} viewed", request.Id.ToString(), user.Email, request.GetIP(), request.GetAgent());

        return dto;
    }
}