using AuthGate.Auth.Application.Features.Users;
using AuthGate.Auth.Presentation.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthGate.Auth.Presentation.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly ISender _mediator;

    public UsersController(ISender mediator, ILogger<UsersController> logger)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet]
    //[HasPermission("CanViewUsers")]
    public async Task<IActionResult> GetAll()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
         Guid.TryParse(userIdStr!, out var userId);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var agent = Request.Headers.UserAgent.ToString();

        _logger.LogInformation("📋 [UsersController] Listing users requested by {UserId} from {Ip}", userId, ip);
        ListUsersQuery query = new ListUsersQuery(userId, ip, agent);

        var list = await _mediator.Send(query);

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [HasPermission("CanViewUsers")]
    public async Task<IActionResult> GetOne([FromServices] GetUserQuery query)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        var userId = Guid.Parse(userIdStr!);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var agent = Request.Headers.UserAgent.ToString();

        query.SetUserInfo(userId, ip, agent);
        _logger.LogInformation("👁️ [UsersController] User {TargetId} details requested by {UserId} ({Ip})", query.GetIP(), userId, ip);

        var user = await _mediator.Send(query);

        return user is null ? NotFound(new { message = "User not found" }) : Ok(user);
    }
}
