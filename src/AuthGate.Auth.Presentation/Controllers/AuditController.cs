using Asp.Versioning;
using AuthGate.Auth.Application.Interfaces;
using AuthGate.Auth.Presentation.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    //[HasPermission("CanViewAudit")]
    public async Task<IActionResult> Get([FromQuery] int limit = 50)
    {
        var logs = await _audit.GetRecentAsync(limit);
        return Ok(logs);
    }
}
