using AuthGate.Auth.Domain.Common;
using AuthGate.Auth.Domain.Enums;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents an audit log entry for tracking security-related actions in the system
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Gets or sets the user who performed the action (null for anonymous actions)
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// Gets or sets the type of action that was performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Gets or sets a description of the action
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the action was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent (browser/client) that performed the action
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the action in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets whether the action was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if the action failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the action (stored as value since audit is in separate DB)
    /// </summary>
    public Guid? UserId { get; set; }
}
