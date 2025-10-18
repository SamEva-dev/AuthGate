using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        // TODO: list device sessions for current user
        return Ok(Array.Empty<object>());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokeOne(Guid id)
    {
        // TODO: revoke device session id (not current)
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> RevokeAllButCurrent()
    {
        // TODO: revoke all except current device
        return NoContent();
    }
}
