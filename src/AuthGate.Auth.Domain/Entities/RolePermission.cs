using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents the many-to-many relationship between roles and permissions
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// Gets or sets the role ID
    /// </summary>
    public required Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the permission ID
    /// </summary>
    public required Guid PermissionId { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the role
    /// </summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets the permission
    /// </summary>
    public virtual Permission Permission { get; set; } = null!;
}
