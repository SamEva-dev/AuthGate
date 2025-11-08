namespace AuthGate.Auth.Domain.Common;

/// <summary>
/// Base entity class providing common properties for all domain entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this entity was created (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this entity was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
