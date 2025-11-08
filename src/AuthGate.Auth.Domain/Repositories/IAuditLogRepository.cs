using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;

namespace AuthGate.Auth.Domain.Repositories;

/// <summary>
/// Repository interface for AuditLog entity operations
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>
    /// Adds a new audit log entry
    /// </summary>
    /// <param name="auditLog">The audit log entry to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs</returns>
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int? skip = null, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by action type
    /// </summary>
    /// <param name="action">The action type</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs</returns>
    Task<IEnumerable<AuditLog>> GetByActionAsync(AuditAction action, int? skip = null, int? take = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range
    /// </summary>
    /// <param name="startDateUtc">Start date (UTC)</param>
    /// <param name="endDateUtc">End date (UTC)</param>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs</returns>
    Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDateUtc, DateTime endDateUtc, int? skip = null, int? take = null, CancellationToken cancellationToken = default);
}
