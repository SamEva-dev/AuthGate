using AuthGate.Auth.Application.Features.AuditLogs.Commands.DeleteAuditLog;
using AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLog;
using AuthGate.Auth.Application.Features.AuditLogs.Queries.GetAuditLogs;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Application.Common.Clients.Models;
using AuthGate.Auth.Application.Common.Models;
using AuthGate.Auth.Authorization;
using AuthGate.Auth.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Authorize(Policy = "NoPasswordChangeRequired")]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILocaGuestProvisioningClient _locaGuest;

    public AuditLogsController(IMediator mediator, ILocaGuestProvisioningClient locaGuest)
    {
        _mediator = mediator;
        _locaGuest = locaGuest;
    }

    public sealed record AuditLogApiDto(
        Guid Id,
        Guid? UserId,
        string Action,
        string? Description,
        string? IpAddress,
        string? UserAgent,
        string? Metadata,
        bool IsSuccess,
        string? ErrorMessage,
        DateTime CreatedAtUtc);

    [HttpGet]
    [HasPermission("auditlogs.read")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        AuditAction? authGateAction = null;
        if (!string.IsNullOrWhiteSpace(action) && Enum.TryParse<AuditAction>(action, ignoreCase: true, out var parsedAction))
        {
            authGateAction = parsedAction;
        }

        var authGateResult = await _mediator.Send(new GetAuditLogsQuery
        {
            Page = 1,
            PageSize = Math.Max(1, (page < 1 ? 1 : page) * (pageSize < 1 ? 50 : pageSize)),
            UserId = userId,
            Action = authGateAction,
            IsSuccess = isSuccess,
            FromUtc = fromUtc,
            ToUtc = toUtc
        });

        if (!authGateResult.IsSuccess)
            return BadRequest(new { message = authGateResult.Error });

        Guid? orgId = null;
        var orgIdStr = User.FindFirstValue("organization_id")
                       ?? User.FindFirstValue("organizationId");
        if (Guid.TryParse(orgIdStr, out var parsedOrgId))
        {
            orgId = parsedOrgId;
        }
        LocaGuestPagedResultDto<LocaGuestAuditLogDto>? locaGuest = null;
        if (orgId.HasValue)
        {
            try
            {
                locaGuest = await _locaGuest.GetAuditLogsAsync(
                    page: 1,
                    pageSize: Math.Max(1, (page < 1 ? 1 : page) * (pageSize < 1 ? 50 : pageSize)),
                    userId: userId,
                    organizationId: orgId,
                    fromUtc: fromUtc,
                    toUtc: toUtc);
            }
            catch
            {
                locaGuest = null;
            }
        }

        var authItems = (authGateResult.Value?.Items ?? new List<AuthGate.Auth.Application.DTOs.Audit.AuditLogDto>())
            .Select(x => new AuditLogApiDto(
                x.Id,
                x.UserId,
                x.Action.ToString(),
                x.Description,
                x.IpAddress,
                x.UserAgent,
                x.Metadata,
                x.IsSuccess,
                x.ErrorMessage,
                x.CreatedAtUtc))
            .ToList();

        var locaItems = (locaGuest?.Items ?? new List<LocaGuestAuditLogDto>())
            .Select(x => new AuditLogApiDto(
                x.Id,
                x.UserId,
                x.Action,
                string.IsNullOrWhiteSpace(x.EntityType)
                    ? null
                    : string.IsNullOrWhiteSpace(x.EntityId)
                        ? x.EntityType
                        : $"{x.EntityType}:{x.EntityId}",
                x.IpAddress,
                x.UserAgent,
                x.AdditionalData,
                (x.StatusCode ?? 200) < 400,
                null,
                x.Timestamp))
            .ToList();

        var merged = authItems
            .Concat(locaItems)
            .Where(x => string.IsNullOrWhiteSpace(action) || string.Equals(x.Action, action, StringComparison.OrdinalIgnoreCase))
            .Where(x => !isSuccess.HasValue || x.IsSuccess == isSuccess.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 50 : pageSize;
        var pageItems = merged
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        var total = merged.Count;

        return Ok(new PagedResult<AuditLogApiDto>
        {
            Items = pageItems,
            TotalCount = total,
            Page = safePage,
            PageSize = safePageSize
        });
    }

    [HttpGet("{id:guid}")]
    [HasPermission("auditlogs.read")]
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
    [HasPermission("auditlogs.delete")]
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
