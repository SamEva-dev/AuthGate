using AuthGate.Auth.Application.Features.Users.Commands.DeleteUser;
using AuthGate.Auth.Application.Features.Users.Commands.UpdateUser;
using AuthGate.Auth.Application.Features.Users.Commands.HardDeleteUser;
using AuthGate.Auth.Application.Features.Users.Commands.ReactivateUser;
using AuthGate.Auth.Application.Features.Users.Queries.GetUserById;
using AuthGate.Auth.Application.Features.Users.Queries.GetUsers;
using AuthGate.Auth.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for user management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    [HttpGet]
    [HasPermission("users.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? role = null)
    {
        var query = new GetUsersQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            IsActive = isActive,
            Role = role
        };

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get user details by ID
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("users.read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update user information
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("users.write")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.UserId)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new { message = "User updated successfully" });
    }

    /// <summary>
    /// Deactivate user (soft delete)
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [HasPermission("users.deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var command = new DeleteUserCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(new { message = "User deactivated successfully" });
    }

    /// <summary>
    /// Reactivate user (set IsActive = true)
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [HasPermission("users.deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReactivateUser(Guid id)
    {
        var command = new ReactivateUserCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(new { message = "User reactivated successfully" });
    }

    /// <summary>
    /// Delete user permanently (hard delete)
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission("users.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var command = new HardDeleteUserCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return NotFound(new { message = result.Error });
        }

        return NoContent();
    }
}
