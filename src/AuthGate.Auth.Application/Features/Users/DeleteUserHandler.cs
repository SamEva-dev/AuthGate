
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Features.Users;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<DeleteUserHandler> _logger;

    public DeleteUserHandler(IUnitOfWork uow, IAuditService audit, ILogger<DeleteUserHandler> logger)
    {
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Auth.GetByIdAsync(request.userId);
        if (user is null)
        {
            _logger.LogWarning("❌ [DeleteUser] User {UserId} not found", request.userId);
            return;
        }

        if (user.IsDeleted)
        {
            _logger.LogWarning("⚠️ [DeleteUser] User {UserId} already deleted", request.userId);
            return;
        }

        await _uow.Auth.DeleteUserAsync(request.userId);
        await _audit.LogAsync("UserDeleted", $"User {user.Email} deleted", request.userId.ToString(), user.Email, request.GetIp(), null);

        _logger.LogInformation("🗑️ [DeleteUser] {Email} deleted by {PerformedBy}", user.Email, request.GetPerformedBy());
    }
}