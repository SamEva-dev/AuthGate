using Asp.Versioning;
using AuthGate.Auth.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthGate.Auth.Presentation.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest req)
        {
            // TODO: create user, hash pwd, email token, persist
            return Ok(); // placeholder
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
        {
            // TODO: validate pwd, lockout check, mfa flow, device session + tokens
            return Ok(); // placeholder
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest req)
        {
            // TODO: validate refresh token in DeviceSessions, rotate, return new tokens
            return Ok(); // placeholder
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            // TODO: generate token + email
            return NoContent();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            // TODO: validate token + set new password
            return NoContent();
        }

        [HttpPost("mfa/enable")]
        public async Task<IActionResult> EnableMfa([FromBody] MfaEnableRequest req)
        {
            // TODO: generate secret + return otpauth url
            return Ok();
        }

        [HttpPost("mfa/verify")]
        public async Task<IActionResult> VerifyMfa([FromBody] MfaVerifyRequest req)
        {
            // TODO: verify code + set MfaEnabled true
            return NoContent();
        }

        [HttpPost("mfa/disable")]
        public async Task<IActionResult> DisableMfa()
        {
            // TODO: clear secret + set MfaEnabled false
            return NoContent();
        }
    }
}
