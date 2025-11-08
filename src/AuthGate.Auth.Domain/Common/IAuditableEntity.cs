namespace AuthGate.Auth.Domain.Common;

/// <summary>
/// Interface for entities that track who created and modified them
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// Date when the entity was created in UTC
    /// </summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Date when the entity was last updated in UTC
    /// </summary>
    DateTime? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the user ID who created this entity
    /// </summary>
    Guid? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the user ID who last updated this entity
    /// </summary>
    Guid? UpdatedBy { get; set; }
}
