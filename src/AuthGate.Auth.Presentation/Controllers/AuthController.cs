using Asp.Versioning;
using AuthGate.Auth.Application.DTOs;
using AuthGate.Auth.Application.Features.Login;
using AuthGate.Auth.Application.Features.Mfa;
using AuthGate.Auth.Application.Features.Password;
using AuthGate.Auth.Application.Features.Refresh;
using AuthGate.Auth.Application.Features.Register;
using AuthGate.Auth.Application.Features.Users;
using AuthGate.Auth.Presentation.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthGate.Auth.Presentation.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ISender _mediator;

        public AuthController(ISender mediator, ILogger<AuthController> logger)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register(
            [FromBody] RegisterCommand req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var agent = Request.Headers.UserAgent.ToString();

            _logger.LogInformation("➡️ [Register] Request from {Ip}, userAgent: {Agent}, email: {Email}", ip, agent, req.Email);

            try
            {
                req.SetIp(ip);
                req.SetUserAgent(agent);
                var response = await _mediator.Send(req);
                _logger.LogInformation("✅ [Register] Success for {Email}", req.Email);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("⚠️ [Register] Conflict for {Email}: {Error}", req.Email, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Register] Unexpected error for {Email}", req.Email);
                return StatusCode(500, new { message = "Unexpected error." });
            }
            finally
            {
                _logger.LogInformation("🏁 [Register] Finished processing {Email}", req.Email);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(
            [FromBody] LoginCommand req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var agent = Request.Headers.UserAgent.ToString();

            _logger.LogInformation("➡️ [Login] Request from {Ip}, userAgent: {Agent}, email: {Email}", ip, agent, req.Email);

            try
            {
                req.SetIp(ip);
                req.SetUserAgent(agent);
                var response = await _mediator.Send(req);
                _logger.LogInformation("✅ [Login] Success for {Email}", req.Email);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("⚠️ [Login] Unauthorized for {Email}: {Error}", req.Email, ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("🚫 [Login] Locked account {Email}: {Error}", req.Email, ex.Message);
                return StatusCode(423, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Login] Unexpected error for {Email}", req.Email);
                return StatusCode(500, new { message = "Unexpected error." });
            }
            finally
            {
                _logger.LogInformation("🏁 [Login] Finished processing {Email}", req.Email);
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshCommand req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var agent = Request.Headers.UserAgent.ToString();

            _logger.LogInformation("➡️ [Refresh] Request from {Ip}", ip);
            try
            {
                req.SetIp(ip);
                req.SetUserAgent(agent);
                var response = await _mediator.Send(req);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("⚠️ [Refresh] Unauthorized: {Msg}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Refresh] Unexpected error");
                return StatusCode(500, new { message = "Unexpected error." });
            }
            finally
            {
                _logger.LogInformation("🏁 [Refresh] Finished");
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var agent = Request.Headers.UserAgent.ToString();

            req.SetIp(ip);
            req.SetUserAgent(agent);
            await _mediator.Send(req);

            return Ok(new { message = "If the email exists, a reset link was sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var agent = Request.Headers.UserAgent.ToString();
            try
            {
                req.SetIp(ip);
                req.SetUserAgent(agent);

                await _mediator.Send(req);
                return Ok(new { message = "Password updated successfully." });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Reset password failed: {Msg}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("mfa/enable")]
        public async Task<IActionResult> EnableMfa([FromBody] EnableMfaCommand req)
        {
            var userId = Guid.Parse(User.FindFirstValue("sub")!);

            req.SetUserId(userId);
            var (secret, qr) = await _mediator.Send(req);

            return Ok(new { secret, qrCodeBase64 = qr });
        }

        [HttpPost("mfa/verify")]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest req)
        {
            // TODO: verify code + set MfaEnabled true
            return NoContent();
        }

        [HttpPost("mfa/verify-login")]
        public async Task<IActionResult> VerifyLoginMfa(
                    [FromBody] VerifyLoginMfaCommand req)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var agent = Request.Headers.UserAgent.ToString();

            try
            {
                req.SetIp(ip);
                req.SetAgent(agent);
                var res = await _mediator.Send(req);
                return Ok(res);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("⚠️ [MFA-Login] Unauthorized: {Msg}", ex.Message);
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("mfa/disable")]
        public async Task<IActionResult> DisableMfa()
        {
            var userId = Guid.Parse(User.FindFirstValue("sub")!);

            DisableMfaCommand disableMfaCommand = new DisableMfaCommand(userId);
            await _mediator.Send(disableMfaCommand);
            return NoContent();
        }

        [HasPermission("CanDeleteUser")]
        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(
            [FromBody] DeleteUserCommand req)
        {
            var requester = User?.FindFirst("email")?.Value ?? "system";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            _logger.LogInformation("➡️ [DeleteUser] Request by {Requester} to delete {UserId}", requester, ip);

            req.SetIp(ip);
            req.SetPerformedBy(requester);
           
            await _mediator.Send(req);

            _logger.LogInformation("✅ [DeleteUser] Request completed by {Requester} for {UserId}", requester, ip);
            return NoContent();
        }
    }
}
