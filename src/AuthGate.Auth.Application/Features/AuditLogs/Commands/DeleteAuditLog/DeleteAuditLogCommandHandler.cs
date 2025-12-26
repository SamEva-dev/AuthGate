using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Application.Features.AuditLogs.Commands.DeleteAuditLog;

public class DeleteAuditLogCommandHandler : IRequestHandler<DeleteAuditLogCommand, Result>
{
    private readonly IAuditDbContext _auditDbContext;

    public DeleteAuditLogCommandHandler(IAuditDbContext auditDbContext)
    {
        _auditDbContext = auditDbContext;
    }

    public async Task<Result> Handle(DeleteAuditLogCommand request, CancellationToken cancellationToken)
    {
        var entity = await _auditDbContext.AuditLogs
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (entity == null)
            return Result.Failure($"AuditLog with ID {request.Id} not found");

        _auditDbContext.AuditLogs.Remove(entity);
        await _auditDbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
