
namespace AuthGate.Auth.Domain.Entities;

public class DeviceSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string RefreshToken { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsRevoked => RevokedAtUtc.HasValue;
}