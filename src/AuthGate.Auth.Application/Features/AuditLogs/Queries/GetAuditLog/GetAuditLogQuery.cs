using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Audit;
using MediatR;

namespace AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLog;

public record GetAuditLogQuery(Guid Id) : IRequest<Result<AuditLogDto>>;
