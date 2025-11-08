using AuthGate.Auth.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AuthGate.Auth.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that handles audit logging for commands marked with IAuditableCommand
/// </summary>
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;

    public AuditBehavior(
        IAuditService auditService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditBehavior<TRequest, TResponse>> logger)
    {
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = default(TResponse);
        var isSuccess = true;
        var errorMessage = string.Empty;

        try
        {
            response = await next();

            // Check if response indicates failure (for Result types)
            if (response is IResult result)
            {
                isSuccess = result.IsSuccess;
                errorMessage = result.Error ?? string.Empty;
            }

            return response;
        }
        catch (Exception ex)
        {
            isSuccess = false;
            errorMessage = ex.Message;
            throw;
        }
        finally
        {
            // Only audit if request implements IAuditableCommand
            if (request is IAuditableCommand auditableCommand)
            {
                try
                {
                    var userId = _httpContextAccessor.GetCurrentUserId();
                    var metadata = JsonSerializer.Serialize(new
                    {
                        RequestType = typeof(TRequest).Name,
                        Timestamp = DateTime.UtcNow
                    });

                    await _auditService.LogAsync(
                        userId,
                        auditableCommand.AuditAction,
                        auditableCommand.GetAuditDescription(),
                        isSuccess,
                        isSuccess ? null : errorMessage,
                        metadata,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    // Don't fail the request if audit logging fails
                    _logger.LogError(ex, "Failed to write audit log for {RequestType}", typeof(TRequest).Name);
                }
            }
        }
    }
}

/// <summary>
/// Interface to check if a result indicates success or failure
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    string? Error { get; }
}
