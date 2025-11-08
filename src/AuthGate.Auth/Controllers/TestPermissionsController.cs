using AuthGate.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Test controller to demonstrate permission-based authorization
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TestPermissionsController : ControllerBase
{
    /// <summary>
    /// Endpoint requiring users.read permission
    /// </summary>
    [HttpGet("users")]
    [HasPermission("users.read")]
    public IActionResult GetUsers()
    {
        return Ok(new { message = "You have users.read permission!", permissions = User.FindAll("permission").Select(c => c.Value) });
    }

    /// <summary>
    /// Endpoint requiring users.write permission
    /// </summary>
    [HttpPost("users")]
    [HasPermission("users.write")]
    public IActionResult CreateUser()
    {
        return Ok(new { message = "You have users.write permission!" });
    }

    /// <summary>
    /// Endpoint requiring users.delete permission
    /// </summary>
    [HttpDelete("users/{id}")]
    [HasPermission("users.delete")]
    public IActionResult DeleteUser(Guid id)
    {
        return Ok(new { message = $"You have users.delete permission! Would delete user {id}" });
    }

    /// <summary>
    /// Endpoint requiring roles.read permission
    /// </summary>
    [HttpGet("roles")]
    [HasPermission("roles.read")]
    public IActionResult GetRoles()
    {
        return Ok(new { message = "You have roles.read permission!" });
    }

    /// <summary>
    /// Endpoint requiring Admin role (not permission-based)
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "You are an Admin!", roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value) });
    }

    /// <summary>
    /// Endpoint available to any authenticated user
    /// </summary>
    [HttpGet("authenticated")]
    public IActionResult AuthenticatedOnly()
    {
        var userClaims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new 
        { 
            message = "You are authenticated!", 
            userId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value,
            email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value,
            roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value),
            permissions = User.FindAll("permission").Select(c => c.Value),
            allClaims = userClaims
        });
    }

    /// <summary>
    /// Endpoint requiring MFA to be enabled
    /// </summary>
    [HttpGet("mfa-required")]
    [Authorize(Policy = "RequireMfa")]
    public IActionResult MfaRequired()
    {
        return Ok(new { message = "You have MFA enabled!" });
    }
}
