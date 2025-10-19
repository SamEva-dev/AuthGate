using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using Microsoft.Extensions.Logging;
using MediatR;

namespace AuthGate.Auth.Application.Features.Refresh;

public class RefreshHandler : IRequestHandler<RefreshCommand, LoginResponse>
{
    private readonly IUnitOfWork _uow;
    private readonly IJwtService _jwt;
    private readonly IAuditService _audit;
    private readonly ILogger<RefreshHandler> _logger;

    public RefreshHandler(IUnitOfWork uow, IJwtService jwt, IAuditService audit, ILogger<RefreshHandler> logger)
    {
        _uow = uow;
        _jwt = jwt;
        _audit = audit;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(RefreshCommand req, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔁 [Refresh] Attempt from {Ip}", req.GetIp());

        // 1) Chercher la session par refreshToken
        var session = await _uow.Auth.GetSessionByRefreshTokenAsync(req.RefreshToken);
        if (session is null)
        {
            _logger.LogWarning("❌ [Refresh] Unknown refresh token");
            await _audit.LogAsync("RefreshFailed", "Unknown refresh token", null, null, req.GetIp(), req.GetUserAgent());
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // 2) Détection de réutilisation / token compromis (reuse)
        if (session.IsRevoked)
        {
            _logger.LogWarning("🛑 [Refresh] Reused revoked token for UserId={UserId}", session.UserId);
            await _audit.LogAsync("RefreshReuseDetected", "Revoked token reused", session.UserId.ToString(), null, req.GetIp(), req.GetUserAgent());

            // Révoquer toute la chaîne (toutes les sessions actives)
            await _uow.Auth.RevokeAllOtherSessionsAsync(session.UserId, Guid.Empty);
            await _uow.SaveChangesAsync();

            throw new UnauthorizedAccessException("Refresh token compromised.");
        }

        if (session.IsExpired)
        {
            _logger.LogWarning("⌛ [Refresh] Expired refresh token for UserId={UserId}", session.UserId);
            await _audit.LogAsync("RefreshFailed", "Expired refresh token", session.UserId.ToString(), null, req.GetIp(), req.GetUserAgent());
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        // 3) Charger l'utilisateur
        var user = await _uow.Auth.GetByIdAsync(session.UserId);
        if (user is null || user.IsDeleted)
        {
            _logger.LogWarning("❌ [Refresh] User not found or deleted: {UserId}", session.UserId);
            await _audit.LogAsync("RefreshFailed", "User not found/deleted", session.UserId.ToString(), null, req.GetIp(), req.GetUserAgent());
            throw new UnauthorizedAccessException("User not found.");
        }

        // 4) Générer nouveaux tokens + créer nouvelle session (rotation)
        var (access, newRefresh, expires) = _jwt.GenerateTokens(user);
        var newSession = new DeviceSession
        {
            UserId = user.Id,
            RefreshToken = newRefresh,
            UserAgent = req.GetUserAgent(),
            IpAddress = req.GetIp(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        await _uow.Auth.AddDeviceSessionAsync(newSession);

        // 5) Révoquer le refresh token précédent (chaînage)
        await _uow.Auth.RevokeSessionAsync(session, replacedByToken: newRefresh);

        // 6) Transaction unique
        await _uow.SaveChangesAsync();

        await _audit.LogAsync("RefreshSuccess", "Refresh token rotated", user.Id.ToString(), user.Email, req.GetIp(), req.GetUserAgent());
        _logger.LogInformation("✅ [Refresh] Success for {Email}", user.Email);

        return new LoginResponse(
            new UserDto(user.Id, user.Email, user.FullName, user.MfaEnabled, user.IsLocked, user.IsDeleted),
            new TokensDto(access, newRefresh, expires)
        );
    }
}