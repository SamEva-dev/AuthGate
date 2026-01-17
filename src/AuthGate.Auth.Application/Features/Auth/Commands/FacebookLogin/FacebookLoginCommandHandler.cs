using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.ExternalLogin;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AuthGate.Auth.Application.Features.Auth.Commands.FacebookLogin;

public class FacebookLoginCommandHandler : IRequestHandler<FacebookLoginCommand, Result<ExternalLoginResponseDto>>
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FacebookLoginCommandHandler> _logger;

    public FacebookLoginCommandHandler(
        IMediator mediator,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<FacebookLoginCommandHandler> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Result<ExternalLoginResponseDto>> Handle(FacebookLoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Facebook OAuth login attempt");

        var accessToken = request.AccessToken ?? request.Token;
        if (string.IsNullOrEmpty(accessToken))
        {
            return Result.Failure<ExternalLoginResponseDto>("Facebook access token is required");
        }

        var userInfo = await VerifyFacebookTokenAsync(accessToken);
        if (userInfo == null)
        {
            return Result.Failure<ExternalLoginResponseDto>("Invalid Facebook token");
        }

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

        return await _mediator.Send(command, cancellationToken);
    }

    private async Task<ExternalUserInfo?> VerifyFacebookTokenAsync(string accessToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var appId = _configuration["OAuth:Facebook:AppId"];
            var appSecret = _configuration["OAuth:Facebook:AppSecret"];

            if (!string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(appSecret))
            {
                var appAccessToken = $"{appId}|{appSecret}";
                var debugUrl = $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appAccessToken)}";

                using var debugResponse = await httpClient.GetAsync(debugUrl);
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

            var url = $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,picture.type(large)&access_token={Uri.EscapeDataString(accessToken)}";
            using var response = await httpClient.GetAsync(url);

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
}
