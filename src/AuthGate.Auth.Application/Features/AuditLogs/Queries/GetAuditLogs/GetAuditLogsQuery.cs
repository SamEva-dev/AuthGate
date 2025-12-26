using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Models;
using AuthGate.Auth.Application.DTOs.Audit;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery : IRequest<Result<PagedResult<AuditLogDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    public Guid? UserId { get; init; }
    public AuditAction? Action { get; init; }
    public bool? IsSuccess { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}
