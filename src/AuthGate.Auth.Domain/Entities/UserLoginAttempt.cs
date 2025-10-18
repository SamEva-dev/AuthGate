
namespace AuthGate.Auth.Domain.Entities;

public class UserLoginAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Email { get; set; } = default!;
    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string IpAddress { get; set; } = default!;
}