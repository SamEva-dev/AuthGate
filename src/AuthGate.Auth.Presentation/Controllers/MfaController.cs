using Asp.Versioning;
using AuthGate.Auth.Application.Features.Mfa;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthGate.Auth.Presentation.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/mfa")]
    [Authorize]
    public class MfaController : ControllerBase
    {
        private readonly ILogger<MfaController> _logger;
        private readonly ISender _mediator;

        public MfaController(ISender mediator, ILogger<MfaController> logger)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpPost("enable")]
        public async Task<IActionResult> Enable(EnableMfaCommand command)
        {
            var userId = Guid.Parse(User.FindFirstValue("sub")!);

            command.SetUserId(userId);
            var (secret, qr) = await _mediator.Send(command);

            return Ok(new { secret, qrCodeBase64 = qr });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(VerifyMfaCommand command
            )
        {
            var userId = Guid.Parse(User.FindFirstValue("sub")!);

            command.SetUserId(userId);
            var success = await _mediator.Send(command);
            return success ? Ok(new { message = "MFA activated" }) : Unauthorized(new { message = "Invalid code" });
        }

        [HttpPost("disable")]
        public async Task<IActionResult> Disable([FromServices] DisableMfaCommand command)
        {
            var userId = Guid.Parse(User.FindFirstValue("sub")!);
            command.SetUserId(userId);

            await _mediator.Send(command);
            return NoContent();
        }
    }
}
