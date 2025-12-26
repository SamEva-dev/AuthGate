using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Common.Models;
using AuthGate.Auth.Application.DTOs.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<PagedResult<AuditLogDto>>>
{
    private readonly IAuditDbContext _auditDbContext;

    public GetAuditLogsQueryHandler(IAuditDbContext auditDbContext)
    {
        _auditDbContext = auditDbContext;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 ? 50 : request.PageSize;

        var query = _auditDbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (request.UserId.HasValue)
            query = query.Where(x => x.UserId == request.UserId.Value);

        if (request.Action.HasValue)
            query = query.Where(x => x.Action == request.Action.Value);

        if (request.IsSuccess.HasValue)
            query = query.Where(x => x.IsSuccess == request.IsSuccess.Value);

        if (request.FromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= request.FromUtc.Value);

        if (request.ToUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= request.ToUtc.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto(
                x.Id,
                x.UserId,
                x.Action,
                x.Description,
                x.IpAddress,
                x.UserAgent,
                x.Metadata,
                x.IsSuccess,
                x.ErrorMessage,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }
}
