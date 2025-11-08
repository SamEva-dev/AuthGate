namespace AuthGate.Auth.Application.DTOs.Users;

/// <summary>
/// Detailed DTO for user information including permissions
/// </summary>
public class UserDetailDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool MfaEnabled { get; set; }
    public bool EmailConfirmed { get; set; }
    public int FailedLoginAttempts { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
