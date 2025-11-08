namespace AuthGate.Auth.Application.DTOs.Roles;

/// <summary>
/// DTO for role information
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
}
