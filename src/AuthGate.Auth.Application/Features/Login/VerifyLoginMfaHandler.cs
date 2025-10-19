using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using MediatR;
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Features.Login;

public class VerifyLoginMfaHandler : IRequestHandler<VerifyLoginMfaCommand, LoginResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IMfaService _mfa;
    private readonly IJwtService _jwt;
    private readonly IAuditService _audit;
    private readonly ILogger<VerifyLoginMfaHandler> _logger;

    public VerifyLoginMfaHandler(
        IUnitOfWork uow,
        IMfaService mfa,
        IJwtService jwt,
        IAuditService audit,
        ILogger<VerifyLoginMfaHandler> logger)
    {
        _uow = uow;
        _mfa = mfa;
        _jwt = jwt;
        _audit = audit;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(VerifyLoginMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Auth.GetByIdAsync(request.UserId);
        if (user is null || user.IsDeleted)
            throw new UnauthorizedAccessException("User not found.");

        if (string.IsNullOrEmpty(user.MfaSecret))
            throw new InvalidOperationException("MFA not configured.");

        if (!_mfa.VerifyCode(user.MfaSecret, request.Code))
        {
            await _audit.LogAsync("MfaLoginFailed", "Invalid MFA code", user.Id.ToString(), user.Email, request.GetIp(), request.GetAgent());
            _logger.LogWarning("❌ Invalid MFA code for {Email}", user.Email);
            throw new UnauthorizedAccessException("Invalid MFA code.");
        }

        var (access, refresh, expires) = _jwt.GenerateTokens(user);

        await _audit.LogAsync("MfaLoginSuccess", "MFA login validated", user.Id.ToString(), user.Email, request.GetIp(), request.GetAgent());
        _logger.LogInformation("✅ MFA login success for {Email}", user.Email);

        return new LoginResponse(
            new UserDto(user.Id, user.Email, user.FullName, user.MfaEnabled, user.IsLocked, user.IsDeleted),
            new TokensDto(access, refresh, expires)
        );
    }
}