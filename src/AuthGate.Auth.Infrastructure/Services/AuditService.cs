using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        Guid? userId,
        AuditAction action,
        string? description = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                Description = description,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                IpAddress = _httpContextAccessor.GetIpAddress(),
                UserAgent = _httpContextAccessor.GetUserAgent(),
                Metadata = metadata,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audit log created: Action={Action}, UserId={UserId}, Success={IsSuccess}",
                action,
                userId,
                isSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action}", action);
        }
    }
}
