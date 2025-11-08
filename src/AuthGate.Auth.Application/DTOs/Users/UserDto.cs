namespace AuthGate.Auth.Application.DTOs.Users;

/// <summary>
/// DTO for user information
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool MfaEnabled { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public List<string> Roles { get; set; } = new();
}
