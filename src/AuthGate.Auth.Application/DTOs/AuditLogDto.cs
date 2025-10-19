
namespace AuthGate.Auth.Application.DTOs;

public class AuditLogDto
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Information";
    public string Message { get; set; } = default!;
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? AuditType { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}