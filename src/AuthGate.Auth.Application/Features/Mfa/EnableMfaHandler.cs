
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;

namespace AuthGate.Auth.Application.Features.Mfa;

public class EnableMfaHandler : IRequestHandler<EnableMfaCommand, (string Secret, string QrCodeBase64)>
{
    private readonly IUnitOfWork _uow;
    private readonly IMfaService _mfa;
    private readonly IAuditService _audit;
    private readonly ILogger<EnableMfaHandler> _logger;

    public EnableMfaHandler(IUnitOfWork uow, IMfaService mfa, IAuditService audit, ILogger<EnableMfaHandler> logger)
    {
        _uow = uow;
        _mfa = mfa;
        _audit = audit;
        _logger = logger;
    }

    public async Task<(string Secret, string QrCodeBase64)> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Auth.GetByIdAsync(request.GetUserId());
        if (user == null || user.IsDeleted)
            throw new InvalidOperationException("User not found.");

        var (secret, qrCode) = _mfa.GenerateSetup(user, request.Issuer);
        user.MfaSecret = secret;
        user.MfaEnabled = false;

        await _uow.SaveChangesAsync();
        await _audit.LogAsync("MfaSetup", "MFA secret generated", user.Id.ToString(), user.Email);

        _logger.LogInformation("🧩 MFA secret generated for {Email}", user.Email);
        return (secret, qrCode);
    }
}
