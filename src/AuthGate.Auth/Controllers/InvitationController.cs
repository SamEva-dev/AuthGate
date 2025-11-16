using AuthGate.Auth.Application.Features.Auth.Commands.AcceptInvitation;
using AuthGate.Auth.Application.Features.Auth.Commands.InviteCollaborator;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvitationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvitationController> _logger;

    public InvitationController(IMediator mediator, ILogger<InvitationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Invite a collaborator to join the organization
    /// Requires TenantOwner or TenantAdmin role
    /// </summary>
    [HttpPost("invite")]
    [Authorize(Roles = "TenantOwner,TenantAdmin")]
    public async Task<IActionResult> InviteCollaborator([FromBody] InviteCollaboratorCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        
        return Ok(result.Value);
    }

    /// <summary>
    /// Accept an invitation and create user account
    /// Public endpoint, no authentication required
    /// </summary>
    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }
        
        return Ok(result.Value);
    }
}
