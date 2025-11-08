using AuthGate.Auth.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;
using AuthGate.Auth.Application.Features.Auth.Commands.PasswordReset.ResetPassword;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for password reset operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class PasswordResetController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(IMediator mediator, ILogger<PasswordResetController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Request a password reset
    /// </summary>
    /// <param name="command">Password reset request details</param>
    /// <returns>Success result</returns>
    [HttpPost("request")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("password-reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetCommand command)
    {
        _logger.LogInformation("Password reset requested for email: {Email}", command.Email);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new 
        { 
            message = "If an account exists with this email, a password reset link has been sent." 
        });
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    /// <param name="command">Password reset details</param>
    /// <returns>Success result</returns>
    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        _logger.LogInformation("Password reset attempt for email: {Email}", command.Email);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new 
        { 
            message = "Password has been reset successfully. You can now log in with your new password." 
        });
    }
}
