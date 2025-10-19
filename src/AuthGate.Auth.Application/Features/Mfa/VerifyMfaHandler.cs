
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Features.Mfa;

public class VerifyMfaHandler : IRequestHandler<VerifyMfaCommand, bool>
{
    private readonly IUnitOfWork _uow;
    private readonly IMfaService _mfa;
    private readonly IAuditService _audit;
    private readonly ILogger<VerifyMfaHandler> _logger;

    public VerifyMfaHandler(IUnitOfWork uow, IMfaService mfa, IAuditService audit, ILogger<VerifyMfaHandler> logger)
    {
        _uow = uow;
        _mfa = mfa;
        _audit = audit;
        _logger = logger;
    }

    public async Task<bool> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Auth.GetByIdAsync(request.GetUserId());    
        if (user == null || user.IsDeleted)
            throw new InvalidOperationException("User not found.");

        if (string.IsNullOrWhiteSpace(user.MfaSecret))
            throw new InvalidOperationException("MFA not set up.");

        var valid = _mfa.VerifyCode(user.MfaSecret, request.Code);
        if (!valid)
        {
            _logger.LogWarning("❌ MFA code invalid for {Email}", user.Email);
            await _audit.LogAsync("MfaVerifyFailed", "Invalid MFA code", user.Id.ToString(), user.Email);
            return false;
        }

        user.MfaEnabled = true;
        user.MfaActivatedAtUtc = DateTime.UtcNow;
        await _uow.SaveChangesAsync();

        await _audit.LogAsync("MfaActivated", "MFA enabled successfully", user.Id.ToString(), user.Email);
        _logger.LogInformation("✅ MFA activated for {Email}", user.Email);
        return true;
    }
}
