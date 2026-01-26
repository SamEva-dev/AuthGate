using System.Security.Cryptography;
using System.Text;
using AuthGate.Auth.Domain.Common;
using AuthGate.Auth.Domain.Enums;

namespace AuthGate.Auth.Domain.Entities;

public class ManagerInvitation : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Email { get; set; } = string.Empty;

    public ManagerInvitationType Type { get; set; }

    public string RoleIdsCsv { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public Guid? ExistingUserId { get; set; }

    public Guid InvitedByUserId { get; set; }

    public ManagerInvitationStatus Status { get; set; } = ManagerInvitationStatus.Pending;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAtUtc;

    public bool IsValid() => Status == ManagerInvitationStatus.Pending && !IsExpired();

    public static (ManagerInvitation Invitation, string RawToken) Create(
        Guid organizationId,
        string email,
        ManagerInvitationType type,
        IEnumerable<Guid> roleIds,
        Guid invitedByUserId,
        TimeSpan expiresIn)
    {
        var id = Guid.NewGuid();
        var rawSecret = GenerateSecureToken();
        var tokenHash = HashToken(rawSecret);

        var invitation = new ManagerInvitation
        {
            Id = id,
            OrganizationId = organizationId,
            Email = email.ToLowerInvariant().Trim(),
            Type = type,
            RoleIdsCsv = string.Join(',', roleIds.Select(r => r.ToString("D"))),
            TokenHash = tokenHash,
            InvitedByUserId = invitedByUserId,
            Status = ManagerInvitationStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.Add(expiresIn),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = invitedByUserId
        };

        var fullToken = $"{id}.{rawSecret}";
        return (invitation, fullToken);
    }

    public bool VerifyToken(string rawSecret)
    {
        var hash = HashToken(rawSecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(TokenHash));
    }

    public void MarkUsed(Guid? updatedBy = null)
    {
        Status = ManagerInvitationStatus.Used;
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Revoke(Guid? updatedBy = null)
    {
        if (Status == ManagerInvitationStatus.Used)
            throw new InvalidOperationException("Cannot revoke a used invitation");

        Status = ManagerInvitationStatus.Revoked;
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
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
}
