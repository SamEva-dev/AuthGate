
using AuthGate.Auth.Application.DTOs;

namespace AuthGate.Auth.Application.Interfaces.Repositories;

public interface IAuditRepository
{
    Task<IEnumerable<AuditLogDto>> GetRecentAsync(int limit = 50);
    Task AddAsync(AuditLogDto entry);
}