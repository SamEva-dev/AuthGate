using Asp.Versioning;
using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Authorize(Roles = "Admin")]
public class PermissionController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _audit;
    private readonly ILogger<PermissionController> _logger;

    public PermissionController(IUnitOfWork uow, IAuditService audit, ILogger<PermissionController> logger)
    {
        _uow = uow;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _uow.Permissions.GetAllAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Permission p)
    {
        await _uow.Permissions.AddAsync(p);
        await _audit.LogAsync("PermissionCreated", $"Permission {p.Code} created");
        _logger.LogInformation("Permission {Code} created", p.Code);
        return CreatedAtAction(nameof(GetAll), new { id = p.Id }, p);
    }

    [HttpPost("{permissionId:guid}/assign/{roleId:guid}")]
    public async Task<IActionResult> Assign(Guid permissionId, Guid roleId)
    {
        await _uow.Permissions.AssignToRoleAsync(roleId, permissionId);
        await _audit.LogAsync("PermissionAssigned", $"Permission {permissionId} assigned to role {roleId}");
        return Ok();
    }

    [HttpDelete("{permissionId:guid}/unassign/{roleId:guid}")]
    public async Task<IActionResult> Unassign(Guid permissionId, Guid roleId)
    {
        await _uow.Permissions.RemoveFromRoleAsync(roleId, permissionId);
        await _audit.LogAsync("PermissionUnassigned", $"Permission {permissionId} unassigned from role {roleId}");
        return Ok();
    }
}
