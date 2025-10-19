
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Features.Mfa;

public class DisableMfaHandler : IRequestHandler<DisableMfaCommand>
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<DisableMfaHandler> _logger;


    public DisableMfaHandler(IUnitOfWork uow, IAuditService audit, ILogger<DisableMfaHandler> logger)
    {
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    public async Task Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Auth.GetByIdAsync(request.GetUserId());
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("❌ Attempt to disable MFA for non-existing user ID {UserId}", request.GetUserId());
            await _audit.LogAsync("MfaDisableFailed", "Attempt to disable MFA for non-existing user", request.GetUserId().ToString(), null);

            throw new InvalidOperationException("User not found.");
        }

        user.MfaEnabled = false;
        user.MfaSecret = null;
        user.MfaActivatedAtUtc = null;
        await _uow.SaveChangesAsync();

        await _audit.LogAsync("MfaDisabled", "MFA disabled", user.Id.ToString(), user.Email);
        _logger.LogInformation("🧨 MFA disabled for {Email}", user.Email);
    }
}