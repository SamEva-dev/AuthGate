using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Application.Features.Auth.Commands.ExternalLogin;
using Google.Apis.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<ExternalLoginResponseDto>>
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IMediator mediator,
        IConfiguration configuration,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<ExternalLoginResponseDto>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Google OAuth login attempt");

        if (string.IsNullOrEmpty(request.Token))
        {
            return Result.Failure<ExternalLoginResponseDto>("Google ID token is required");
        }

        var userInfo = await VerifyGoogleTokenAsync(request.Token);
        if (userInfo == null)
        {
            return Result.Failure<ExternalLoginResponseDto>("Invalid Google token");
        }

        var command = new ExternalLoginCommand
        {
            Provider = "google",
            Token = request.Token,
            Email = userInfo.Email,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            ProviderId = userInfo.ProviderId,
            PictureUrl = userInfo.PictureUrl
        };

        return await _mediator.Send(command, cancellationToken);
    }

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
}
