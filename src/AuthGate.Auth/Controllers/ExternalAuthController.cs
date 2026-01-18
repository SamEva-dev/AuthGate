using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.ExternalLogin;
using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for external OAuth authentication (Google, Facebook)
/// </summary>
[ApiController]
[Route("api/external-auth")]
public class ExternalAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExternalAuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ExternalAuthController(
        IMediator mediator,
        ILogger<ExternalAuthController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Get available OAuth providers configuration
    /// </summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OAuthProvidersResponseDto), StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        var googleClientId = _configuration["OAuth:Google:ClientId"];
        var facebookAppId = _configuration["OAuth:Facebook:AppId"];

        var providers = new OAuthProvidersResponseDto
        {
            Providers = new List<OAuthProviderInfo>
            {
                new OAuthProviderInfo
                {
                    Name = "google",
                    DisplayName = "Google",
                    Enabled = !string.IsNullOrEmpty(googleClientId),
                    IconUrl = "https://www.google.com/favicon.ico"
                },
                new OAuthProviderInfo
                {
                    Name = "facebook",
                    DisplayName = "Facebook",
                    Enabled = !string.IsNullOrEmpty(facebookAppId),
                    IconUrl = "https://www.facebook.com/favicon.ico"
                }
            }
        };

        return Ok(providers);
    }

    /// <summary>
    /// Get Google OAuth configuration for frontend
    /// </summary>
    [HttpGet("google/config")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetGoogleConfig()
    {
        var clientId = _configuration["OAuth:Google:ClientId"];
        
        if (string.IsNullOrEmpty(clientId))
        {
            return BadRequest(new { error = "Google OAuth is not configured" });
        }

        return Ok(new
        {
            clientId,
            scope = "openid email profile"
        });
    }

    /// <summary>
    /// Get Facebook OAuth configuration for frontend
    /// </summary>
    [HttpGet("facebook/config")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetFacebookConfig()
    {
        var appId = _configuration["OAuth:Facebook:AppId"];
        
        if (string.IsNullOrEmpty(appId))
        {
            return BadRequest(new { error = "Facebook OAuth is not configured" });
        }

        return Ok(new
        {
            appId,
            scope = "email,public_profile"
        });
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
        try
        {
            _logger.LogInformation("Google OAuth login attempt");

            if (string.IsNullOrEmpty(dto.Token))
            {
                return BadRequest(new { error = "Google ID token is required" });
            }

            // Verify Google ID token and extract user info
            var userInfo = await VerifyGoogleTokenAsync(dto.Token);
            if (userInfo == null)
            {
                return Unauthorized(new { error = "Invalid Google token" });
            }

            // Process login via command handler
            var command = new ExternalLoginCommand
            {
                Provider = "google",
                Token = dto.Token,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                ProviderId = userInfo.ProviderId,
                PictureUrl = userInfo.PictureUrl
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google OAuth login");
            return StatusCode(500, new { error = "An error occurred during Google login" });
        }
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
        try
        {
            _logger.LogInformation("Facebook OAuth login attempt");

            var accessToken = dto.AccessToken ?? dto.Token;
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { error = "Facebook access token is required" });
            }

            // Verify Facebook token and get user info
            var userInfo = await VerifyFacebookTokenAsync(accessToken);
            if (userInfo == null)
            {
                return Unauthorized(new { error = "Invalid Facebook token" });
            }

            // Process login via command handler
            var command = new ExternalLoginCommand
            {
                Provider = "facebook",
                Token = accessToken,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                ProviderId = userInfo.ProviderId,
                PictureUrl = userInfo.PictureUrl
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return BadRequest(new { error = result.Error });
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Facebook OAuth login");
            return StatusCode(500, new { error = "An error occurred during Facebook login" });
        }
    }

    #region Token Verification Helpers

    private async Task<ExternalUserInfo?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var expectedClientId = _configuration["OAuth:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(expectedClientId))
            {
                _logger.LogWarning("Google OAuth is not configured (missing OAuth:Google:ClientId)");
                return null;
            }

            // Validate JWT signature + expiry locally (no outbound HTTP call)
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { expectedClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            if (payload.EmailVerified != true)
            {
                _logger.LogWarning("Google email not verified for subject {Sub}", payload.Subject);
                return null;
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                _logger.LogWarning("Google token does not contain an email claim");
                return null;
            }

            return new ExternalUserInfo
            {
                Email = payload.Email ?? string.Empty,
                FirstName = payload.GivenName ?? string.Empty,
                LastName = payload.FamilyName ?? string.Empty,
                ProviderId = payload.Subject ?? string.Empty,
                PictureUrl = payload.Picture
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google ID token");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Google token");
            return null;
        }
    }

    private async Task<ExternalUserInfo?> VerifyFacebookTokenAsync(string accessToken)
    {
        try
        {
            var appId = _configuration["OAuth:Facebook:AppId"];
            var appSecret = _configuration["OAuth:Facebook:AppSecret"];

            // 1) Validate the token is valid and issued for *our* app (debug_token)
            if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(appSecret))
            {
                var appAccessToken = $"{appId}|{appSecret}";
                var debugUrl = $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";

                using var debugResponse = await _httpClient.GetAsync(debugUrl);
                if (!debugResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Facebook debug_token failed with status {Status}", debugResponse.StatusCode);
                    return null;
                }

                var debugContent = await debugResponse.Content.ReadAsStringAsync();
                using var debugDoc = JsonDocument.Parse(debugContent);

                if (!debugDoc.RootElement.TryGetProperty("data", out var dataEl))
                {
                    _logger.LogWarning("Facebook debug_token response missing 'data'");
                    return null;
                }

                var isValid = dataEl.TryGetProperty("is_valid", out var isValidEl) && isValidEl.ValueKind == JsonValueKind.True;
                if (!isValid)
                {
                    _logger.LogWarning("Facebook token is not valid");
                    return null;
                }

                if (dataEl.TryGetProperty("app_id", out var appIdEl))
                {
                    var tokenAppId = appIdEl.GetString();
                    if (!string.Equals(tokenAppId, appId, StringComparison.Ordinal))
                    {
                        _logger.LogWarning("Facebook token app_id mismatch. Expected {Expected}, got {Actual}", appId, tokenAppId);
                        return null;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Facebook OAuth is not fully configured (missing OAuth:Facebook:AppId/AppSecret). Proceeding without debug_token validation.");
            }

            // 2) Fetch user profile
            var url = $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture.type(large)&access_token={Uri.EscapeDataString(accessToken)}";
            using var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook API call failed with status {Status}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("email", out var emailEl))
            {
                _logger.LogWarning("Facebook response does not contain email");
                return null;
            }

            var email = emailEl.GetString();
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("Facebook email is empty");
                return null;
            }

            var id = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;
            var firstName = doc.RootElement.GetProperty("first_name").GetString() ?? string.Empty;
            var lastName = doc.RootElement.GetProperty("last_name").GetString() ?? string.Empty;

            string? pictureUrl = null;
            if (doc.RootElement.TryGetProperty("picture", out var pictureEl) &&
                pictureEl.TryGetProperty("data", out var pictureDataEl) &&
                pictureDataEl.TryGetProperty("url", out var pictureUrlEl))
            {
                pictureUrl = pictureUrlEl.GetString();
            }

            return new ExternalUserInfo
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                ProviderId = id,
                PictureUrl = pictureUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Facebook token");
            return null;
        }
    }

    #endregion
}
