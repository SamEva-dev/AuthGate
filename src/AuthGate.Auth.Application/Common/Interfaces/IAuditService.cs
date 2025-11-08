using AuthGate.Auth.Domain.Enums;

namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Service interface for recording audit logs
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records an audit log entry
    /// </summary>
    /// <param name="userId">ID of the user performing the action (null for anonymous)</param>
    /// <param name="action">Type of action performed</param>
    /// <param name="description">Description of the action</param>
    /// <param name="isSuccess">Whether the action was successful</param>
    /// <param name="errorMessage">Error message if action failed</param>
    /// <param name="metadata">Additional metadata in JSON format</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogAsync(
        Guid? userId,
        AuditAction action,
        string? description = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}
