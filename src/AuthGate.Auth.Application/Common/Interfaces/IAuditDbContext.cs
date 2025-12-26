using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Application.Common.Interfaces;

public interface IAuditDbContext
{
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
