using AuthGate.Auth.Application.Features.AuditLogs.Commands.DeleteAuditLog;
using AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLog;
using AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLogs;
using AuthGate.Auth.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null,
        [FromQuery] AuditAction? action = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var result = await _mediator.Send(new GetAuditLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            Action = action,
            IsSuccess = isSuccess,
            FromUtc = fromUtc,
            ToUtc = toUtc
        });

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetAuditLogQuery(id));

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteAuditLogCommand(id));

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { message = result.Error });
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "Audit log deleted successfully", id });
    }
}
