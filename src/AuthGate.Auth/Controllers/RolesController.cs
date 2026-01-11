using AuthGate.Auth.Application.Features.Roles.Commands.AssignPermissionToRole;
using AuthGate.Auth.Application.Features.Roles.Commands.RemovePermissionFromRole;
using AuthGate.Auth.Application.Features.Roles.Queries.GetRoles;
using AuthGate.Auth.Authorization;
using AuthGate.Auth.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.TenantOwner}")]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [HasPermission("roles.read")]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _mediator.Send(new GetRolesQuery());
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{roleId}/permissions/{permissionId}")]
    [HasPermission("permissions.write")]
    public async Task<IActionResult> AssignPermission(Guid roleId, Guid permissionId)
    {
        var result = await _mediator.Send(new AssignPermissionToRoleCommand(roleId, permissionId));
        return result.IsSuccess ? Ok(new { message = "Permission assigned" }) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{roleId}/permissions/{permissionId}")]
    [HasPermission("permissions.write")]
    public async Task<IActionResult> RemovePermission(Guid roleId, Guid permissionId)
    {
        var result = await _mediator.Send(new RemovePermissionFromRoleCommand(roleId, permissionId));
        return result.IsSuccess ? NoContent() : NotFound(new { message = result.Error });
    }
}
