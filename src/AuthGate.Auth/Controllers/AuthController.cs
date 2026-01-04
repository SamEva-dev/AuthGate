using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.AcceptInvitation;
using AuthGate.Auth.Application.Features.Auth.Commands.InviteCollaborator;
using AuthGate.Auth.Application.Features.Auth.Commands.Login;
using AuthGate.Auth.Application.Features.Auth.Commands.AcceptLocaGuestInvitation;
using AuthGate.Auth.Application.Features.Auth.Commands.RefreshToken;
using AuthGate.Auth.Application.Features.Auth.Commands.Register;
using AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;
using AuthGate.Auth.Application.Features.Auth.Commands.Verify2FA;
using AuthGate.Auth.Application.Features.Auth.Commands.VerifyRecoveryCode;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public AuthController(
        IMediator mediator, 
        ILogger<AuthController> logger,
        UserManager<User> userManager,
        RoleManager<Role> roleManager)
    {
        _mediator = mediator;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("invitations/accept")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AcceptLocaGuestInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptLocaGuestInvitation([FromBody] AcceptLocaGuestInvitationCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("prelogin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PreLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PreLogin([FromBody] PreLoginRequestDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { error = "Email is required." });

        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null)
        {
            return Ok(new PreLoginResponseDto
            {
                NextStep = "Register"
            });
        }

        if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
        {
            return Ok(new PreLoginResponseDto
            {
                NextStep = "Error",
                Error = "Account has no organization assigned. Please contact support or use an invitation link."
            });
        }

        return Ok(new PreLoginResponseDto
        {
            NextStep = "Password"
        });
    }

    /// <summary>
    /// Register a new organization owner (TenantOwner) with automatic organization creation
    /// This creates both an organization in LocaGuest and a user in AuthGate
    /// </summary>
    [HttpPost("register-with-tenant")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterWithTenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterWithTenant([FromBody] RegisterWithTenantCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Verify 2FA/TOTP code and complete login
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FACommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Verify 2FA recovery code and complete login
    /// </summary>
    [HttpPost("verify-recovery-code")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyRecoveryCode([FromBody] VerifyRecoveryCodeCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = dto.RefreshToken
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Logout and revoke refresh token
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        // TODO: Implement logout command to revoke refresh token
        return NoContent();
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Essayer d'abord avec 'sub' (standard JWT), puis avec ClaimTypes.NameIdentifier
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No user ID found in claims. Available claims: {Claims}", 
                string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Get user permissions from roles
        var permissions = new List<string>();
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null && role.RolePermissions != null)
            {
                permissions.AddRange(role.RolePermissions.Select(rp => rp.Permission.Code));
            }
        }

        var currentUserDto = new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            MfaEnabled = user.MfaEnabled,
            Roles = roles.ToList(),
            Permissions = permissions.Distinct().ToList(),
            CreatedAt = user.CreatedAtUtc,
            LastLoginAt = user.LastLoginAtUtc
        };

        return Ok(currentUserDto);
    }

    /// <summary>
    /// Invite a collaborator to join the organization
    /// Requires TenantOwner or TenantAdmin role
    /// </summary>
    [HttpPost("invite")]
    [Authorize]
    [ProducesResponseType(typeof(InviteCollaboratorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteCollaborator([FromBody] InviteCollaboratorCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Invitation sent to {Email}", command.Email);
        return Ok(result.Value);
    }

    /// <summary>
    /// Accept an invitation and create a user account
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpPost("accept-invitation")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AcceptInvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitation([FromBody] AcceptInvitationCommand command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Invitation accepted by {Email}", result.Value.Email);
        return Ok(result.Value);
    }
}
