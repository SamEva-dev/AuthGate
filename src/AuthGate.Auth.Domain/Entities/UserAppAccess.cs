using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

public class UserAppAccess : IAuditableEntity
{
    public Guid UserId { get; set; }
    public string AppId { get; set; } = string.Empty;

    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? GrantedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public virtual User? User { get; set; }
}
