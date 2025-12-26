using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLog;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, Result<AuditLogDto>>
{
    private readonly IAuditDbContext _auditDbContext;

    public GetAuditLogQueryHandler(IAuditDbContext auditDbContext)
    {
        _auditDbContext = auditDbContext;
    }

    public async Task<Result<AuditLogDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var entity = await _auditDbContext.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            return Result.Failure<AuditLogDto>($"AuditLog with ID {request.Id} not found");

        return Result.Success(new AuditLogDto(
            entity.Id,
            entity.UserId,
            entity.Action,
            entity.Description,
            entity.IpAddress,
            entity.UserAgent,
            entity.Metadata,
            entity.IsSuccess,
            entity.ErrorMessage,
            entity.CreatedAtUtc));
    }
}
