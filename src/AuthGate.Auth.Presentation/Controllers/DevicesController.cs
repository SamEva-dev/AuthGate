using Asp.Versioning;
using AuthGate.Auth.Application.Features.Devices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthGate.Auth.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(ISender mediator, ILogger<DevicesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        var query = new ListDevicesQuery(userId);
        var res = await _mediator.Send(query);
        return Ok(res);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeOne(Guid id, [FromServices] RevokeOneDeviceCommand command)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        command.SetIp(ip);
        command.SetUserId(userId);

        await _mediator.Send(command);

        return NoContent();
    }

    [HttpDelete("others/{currentId:guid}")]
    public async Task<IActionResult> RevokeOthers(Guid currentId, [FromServices] RevokeOthersDeviceCommand command)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        command.SetIp(ip);
        command.SetUserId(userId);

        await _mediator.Send(command);

        return NoContent();
    }
}
