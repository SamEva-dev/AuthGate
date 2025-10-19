using Asp.Versioning;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AuthGate.Auth.Presentation.Security;

namespace AuthGate.Auth.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize(Roles = "Admin")]
public class RoleController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<RoleController> _logger;

    public RoleController(IUnitOfWork uow, IAuditService audit, ILogger<RoleController> logger)
    {
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _uow.Roles.GetAllAsync();
        return Ok(roles);
    }

    [HttpPost]
    [HasPermission("CanManageRoles")]
    public async Task<IActionResult> Create([FromBody] Role role)
    {
        await _uow.Roles.AddAsync(role);
        await _audit.LogAsync("RoleCreated", $"Role {role.Name} created", null, null, HttpContext.Connection.RemoteIpAddress?.ToString());
        _logger.LogInformation("Role {Name} created", role.Name);
        return CreatedAtAction(nameof(GetAll), new { id = role.Id }, role);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _uow.Roles.DeleteAsync(id);
        await _audit.LogAsync("RoleDeleted", $"Role {id} deleted", null, null, HttpContext.Connection.RemoteIpAddress?.ToString());
        _logger.LogInformation("Role {Id} deleted", id);
        return NoContent();
    }

    [HttpPost("{roleId:guid}/assign/{userId:guid}")]
    public async Task<IActionResult> Assign(Guid roleId, Guid userId)
    {
        await _uow.Roles.AssignRoleAsync(userId, roleId);
        await _audit.LogAsync("RoleAssigned", $"Role {roleId} assigned to {userId}", userId.ToString());
        _logger.LogInformation("Role {RoleId} assigned to {UserId}", roleId, userId);
        return Ok();
    }

    [HttpDelete("{roleId:guid}/unassign/{userId:guid}")]
    public async Task<IActionResult> Unassign(Guid roleId, Guid userId)
    {
        await _uow.Roles.RemoveRoleAsync(userId, roleId);
        await _audit.LogAsync("RoleUnassigned", $"Role {roleId} unassigned from {userId}", userId.ToString());
        _logger.LogInformation("Role {RoleId} unassigned from {UserId}", roleId, userId);
        return Ok();
    }
}
