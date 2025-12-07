using AuthGate.Auth.Application.Features.TwoFactor.Commands.DisableTwoFactor;
using AuthGate.Auth.Application.Features.TwoFactor.Commands.EnableTwoFactor;
using AuthGate.Auth.Application.Features.TwoFactor.Commands.VerifyAndEnableTwoFactor;
using AuthGate.Auth.Application.Features.TwoFactor.Queries.GetTwoFactorStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for Two-Factor Authentication (2FA) operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TwoFactorController : ControllerBase
{
    private readonly IMediator _mediator;

    public TwoFactorController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Gets the 2FA status for the current user
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TwoFactorStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var query = new GetTwoFactorStatusQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Initializes 2FA (generates QR code and recovery codes)
    /// </summary>
    [HttpPost("enable")]
    [ProducesResponseType(typeof(EnableTwoFactorResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Enable()
    {
        var command = new EnableTwoFactorCommand();
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Verifies TOTP code and enables 2FA
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyAndEnable([FromBody] VerifyRequest request)
    {
        var command = new VerifyAndEnableTwoFactorCommand { Code = request.Code };
        
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { success = result, message = "Two-factor authentication enabled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Disables 2FA (requires password confirmation)
    /// </summary>
    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable([FromBody] DisableRequest request)
    {
        var command = new DisableTwoFactorCommand { Password = request.Password };
        
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { success = result, message = "Two-factor authentication disabled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request DTO for verifying 2FA code
/// </summary>
public record VerifyRequest(string Code);

/// <summary>
/// Request DTO for disabling 2FA
/// </summary>
public record DisableRequest(string Password);
