using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.FacebookLogin;
using AuthGate.Auth.Application.Features.Auth.Commands.GoogleLogin;
using AuthGate.Auth.Application.Features.Auth.Queries.GetFacebookConfig;
using AuthGate.Auth.Application.Features.Auth.Queries.GetGoogleConfig;
using AuthGate.Auth.Application.Features.Auth.Queries.GetOAuthProviders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for external OAuth authentication (Google, Facebook)
/// </summary>
[ApiController]
[Route("api/external-auth")]
public class ExternalAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExternalAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get available OAuth providers configuration
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OAuthProvidersResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders()
    {
        var result = await _mediator.Send(new GetOAuthProvidersQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get Google OAuth configuration for frontend
    /// </summary>
    [HttpGet("google/config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OAuthConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGoogleConfig()
    {
        var result = await _mediator.Send(new GetGoogleConfigQuery());
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get Facebook OAuth configuration for frontend
    /// </summary>
    [HttpGet("facebook/config")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OAuthConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFacebookConfig()
    {
        var result = await _mediator.Send(new GetFacebookConfigQuery());
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Authenticate with Google ID token (from frontend Google Sign-In)
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExternalLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginWithGoogle([FromBody] ExternalTokenLoginDto dto)
    {
        var command = new GoogleLoginCommand { Token = dto.Token };
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Authenticate with Facebook access token (from frontend Facebook Login)
    /// </summary>
    [HttpPost("facebook")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExternalLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginWithFacebook([FromBody] ExternalTokenLoginDto dto)
    {
        var command = new FacebookLoginCommand 
        { 
            Token = dto.Token,
            AccessToken = dto.AccessToken
        };
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
