using AuthGate.Auth.Application.Features.Permissions.Queries.GetPermissions;
using AuthGate.Auth.Authorization;
using AuthGate.Auth.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.TenantOwner}")]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission("permissions.read")]
    public async Task<IActionResult> GetPermissions()
    {
        var result = await _mediator.Send(new GetPermissionsQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }
}
