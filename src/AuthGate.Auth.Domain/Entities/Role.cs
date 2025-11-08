using AuthGate.Auth.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents a role that can be assigned to users for authorization, extending IdentityRole for hybrid approach
/// </summary>
public class Role : IdentityRole<Guid>, IAuditableEntity
{
    /// <summary>
    /// Gets or sets the role description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether this is a system role (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public DateTime? UpdatedAtUtc { get; set; }

    /// <inheritdoc/>
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc/>
    public Guid? UpdatedBy { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the collection of role permissions
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
