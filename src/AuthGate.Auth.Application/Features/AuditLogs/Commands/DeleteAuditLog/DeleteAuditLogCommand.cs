using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.AuditLogs.Commands.DeleteAuditLog;

public record DeleteAuditLogCommand(Guid Id) : IRequest<Result>;
