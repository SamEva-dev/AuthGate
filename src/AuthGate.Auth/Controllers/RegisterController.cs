using AuthGate.Auth.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for user registration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class RegisterController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RegisterController> _logger;

    public RegisterController(IMediator mediator, ILogger<RegisterController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="command">Registration details</param>
    /// <returns>Registration result with user information</returns>
    [HttpPost]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        _logger.LogInformation("Registration request received for email: {Email}", command.Email);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }
}
