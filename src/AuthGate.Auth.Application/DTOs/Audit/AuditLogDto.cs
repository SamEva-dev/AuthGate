using AuthGate.Auth.Domain.Enums;

namespace AuthGate.Auth.Application.DTOs.Audit;

public record AuditLogDto(
    Guid Id,
    Guid? UserId,
    AuditAction Action,
    string? Description,
    string? IpAddress,
    string? UserAgent,
    string? Metadata,
    bool IsSuccess,
    string? ErrorMessage,
    DateTime CreatedAtUtc
);
