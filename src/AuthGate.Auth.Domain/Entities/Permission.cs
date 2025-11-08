using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a permission that can be granted to roles for fine-grained authorization
/// </summary>
public class Permission : BaseEntity, IAuditableEntity
{
    /// <summary>
    /// Gets or sets the unique code identifier for this permission (e.g., "users.read", "roles.write")
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Gets or sets the normalized code for case-insensitive lookups
    /// </summary>
    public required string NormalizedCode { get; set; }

    /// <summary>
    /// Gets or sets the display name of the permission
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description of what this permission allows
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category or group this permission belongs to (e.g., "Users", "Roles")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether this permission is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <inheritdoc/>
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc/>
    public Guid? UpdatedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the collection of roles that have this permission
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
