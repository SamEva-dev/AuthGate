using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

public class UserOrganization : IAuditableEntity
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }

    public string? RoleInOrg { get; set; }

    public string? OrganizationDisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public virtual User? User { get; set; }
}
