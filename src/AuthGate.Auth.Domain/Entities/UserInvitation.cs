using AuthGate.Auth.Domain.Common;

namespace AuthGate.Auth.Domain.Entities;

/// <summary>
/// Represents an invitation for a collaborator to join an organization
/// </summary>
public class UserInvitation : IAuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Organization (Tenant) ID from LocaGuest
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Tenant code (T0001, T0002...)
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// Email of the invited user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Role to assign (TenantAdmin, TenantManager, TenantUser, ReadOnly)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Unique secure token (JWT or GUID)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User who created the invitation (inviter)
    /// </summary>
    public Guid InvitedBy { get; set; }

    /// <summary>
    /// Invitation status
    /// </summary>
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>
    /// Expiration date (typically 7 days)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Date when invitation was accepted
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// User ID if invitation was accepted
    /// </summary>
    public Guid? AcceptedByUserId { get; set; }

    /// <summary>
    /// Optional personal message
    /// </summary>
    public string? Message { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    public bool IsValid() => Status == InvitationStatus.Pending && !IsExpired();

    public void Accept(Guid userId)
    {
        if (!IsValid())
            throw new InvalidOperationException("Invitation is not valid");

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        AcceptedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == InvitationStatus.Accepted)
            throw new InvalidOperationException("Cannot cancel an accepted invitation");

        Status = InvitationStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status == InvitationStatus.Pending)
        {
            Status = InvitationStatus.Expired;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired,
    Cancelled
}
