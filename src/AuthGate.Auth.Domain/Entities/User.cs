
namespace AuthGate.Auth.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool IsEmailValidated { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockoutEndUtc { get; set; }

    public ICollection<DeviceSession> DeviceSessions { get; set; } = new List<DeviceSession>();
    public ICollection<UserLoginAttempt> LoginAttempts { get; set; } = new List<UserLoginAttempt>();
}