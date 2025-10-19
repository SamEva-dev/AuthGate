
using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repo;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditRepository repo, ILogger<AuditService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task LogAsync(string auditType, string message, string? userId = null, string? email = null, string? ip = null, string? userAgent = null)
    {
        var entry = new AuditLogDto
        {
            Timestamp = DateTime.UtcNow,
            Level = "Information",
            Message = message,
            AuditType = auditType,
            UserId = userId,
            Email = email,
            IpAddress = ip,
            UserAgent = userAgent
        };

        await _repo.AddAsync(entry);
        _logger.LogInformation("🪵 [AUDIT] {AuditType}: {Message}", auditType, message);
    }

    public Task<IEnumerable<AuditLogDto>> GetRecentAsync(int limit = 50)
        => _repo.GetRecentAsync(limit);
}