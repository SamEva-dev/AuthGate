using AuthGate.Auth.Application.DTOs;

namespace AuthGate.Auth.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string auditType, string message, string? userId = null, string? email = null, string? ip = null, string? userAgent = null);
    Task<IEnumerable<AuditLogDto>> GetRecentAsync(int limit = 50);
}