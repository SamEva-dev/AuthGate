using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.ExternalLogin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AuthGate.Auth.Controllers;

/// <summary>
/// Controller for external OAuth authentication (Google, Facebook)
/// </summary>
[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExternalAuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExternalAuthController(
        IMediator mediator,
        ILogger<ExternalAuthController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
            var httpClient = _httpClientFactory.CreateClient();
            
            // Verify token with Google's tokeninfo endpoint
            var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token verification failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = JsonDocument.Parse(content);
            var root = tokenInfo.RootElement;

            // Verify audience (client ID)
            var expectedClientId = _configuration["OAuth:Google:ClientId"];
            var audience = root.TryGetProperty("aud", out var audProp) ? audProp.GetString() : null;
            
            if (audience != expectedClientId)
            {
                _logger.LogWarning("Google token audience mismatch: expected {Expected}, got {Actual}", expectedClientId, audience);
                return null;
            }

            return new ExternalUserInfo
            {
                Email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() ?? "" : "",
                FirstName = root.TryGetProperty("given_name", out var givenNameProp) ? givenNameProp.GetString() ?? "" : "",
                LastName = root.TryGetProperty("family_name", out var familyNameProp) ? familyNameProp.GetString() ?? "" : "",
                ProviderId = root.TryGetProperty("sub", out var subProp) ? subProp.GetString() ?? "" : "",
                PictureUrl = root.TryGetProperty("picture", out var pictureProp) ? pictureProp.GetString() : null
            };
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
            var httpClient = _httpClientFactory.CreateClient();
            
            // Get user info from Facebook Graph API
            var response = await httpClient.GetAsync(
                $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture.type(large)&access_token={accessToken}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook token verification failed: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonDocument.Parse(content);
            var root = userInfo.RootElement;

            // Check if we got an email (required for our system)
            var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Facebook user did not grant email permission");
                return null;
            }

            string? pictureUrl = null;
            if (root.TryGetProperty("picture", out var pictureProp) && 
                pictureProp.TryGetProperty("data", out var dataProp) &&
                dataProp.TryGetProperty("url", out var urlProp))
            {
                pictureUrl = urlProp.GetString();
            }

            return new ExternalUserInfo
            {
                Email = email,
                FirstName = root.TryGetProperty("first_name", out var firstNameProp) ? firstNameProp.GetString() ?? "" : "",
                LastName = root.TryGetProperty("last_name", out var lastNameProp) ? lastNameProp.GetString() ?? "" : "",
                ProviderId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "",
                PictureUrl = pictureUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Facebook token");
            return null;
        }
    }

    private class ExternalUserInfo
    {
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string ProviderId { get; set; } = "";
        public string? PictureUrl { get; set; }
    }

    #endregion
}
