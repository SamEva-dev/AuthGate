using System.Security.Cryptography;
using System.Text;
using AuthGate.Auth.Domain.Common;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Domain.Enums;

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
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Tenant code (T0001, T0002...)
    /// </summary>
    public string OrganizationCode { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>
    /// Email of the invited user
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Role to assign (TenantAdmin, TenantManager, TenantUser, ReadOnly)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Hashed token for secure storage (SHA256)
    /// The raw token is only sent once to the invitee
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

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

    /// <summary>
    /// Validates that the role is an allowed invitation role
    /// </summary>
    public static bool IsValidRole(string role)
    {
        return role == Roles.TenantAdmin ||
               role == Roles.TenantManager ||
               role == Roles.TenantUser ||
               role == Roles.ReadOnly;
    }

    /// <summary>
    /// Creates a new invitation with a generated token
    /// Returns the raw token (to send to invitee) - only available at creation time
    /// </summary>
    public static (UserInvitation Invitation, string RawToken) Create(
        Guid organizationId,
        string organizationCode,
        string organizationName,
        string email,
        string role,
        Guid invitedBy,
        string? message = null,
        int expirationDays = 7)
    {
        if (!IsValidRole(role))
            throw new ArgumentException($"Invalid role: {role}. Must be one of: TenantAdmin, TenantManager, TenantUser, ReadOnly", nameof(role));

        var id = Guid.NewGuid();
        var rawToken = GenerateSecureToken();
        var tokenHash = HashToken(rawToken);

        var invitation = new UserInvitation
        {
            Id = id,
            OrganizationId = organizationId,
            OrganizationCode = organizationCode,
            OrganizationName = organizationName,
            Email = email.ToLowerInvariant().Trim(),
            Role = role,
            TokenHash = tokenHash,
            InvitedBy = invitedBy,
            Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
            Message = message,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = invitedBy
        };

        // Return format: {id}.{token} for URL construction
        var fullToken = $"{id}.{rawToken}";
        return (invitation, fullToken);
    }

    /// <summary>
    /// Verifies if the provided token matches this invitation
    /// </summary>
    public bool VerifyToken(string rawToken)
    {
        var hash = HashToken(rawToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(TokenHash));
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

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
