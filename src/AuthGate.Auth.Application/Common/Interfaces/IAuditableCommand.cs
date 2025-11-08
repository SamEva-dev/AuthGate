using AuthGate.Auth.Domain.Enums;

namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that require audit logging
/// </summary>
public interface IAuditableCommand
{
    /// <summary>
    /// Gets the audit action type for this command
    /// </summary>
    AuditAction AuditAction { get; }

    /// <summary>
    /// Gets the description for the audit log
    /// </summary>
    string GetAuditDescription();
}
