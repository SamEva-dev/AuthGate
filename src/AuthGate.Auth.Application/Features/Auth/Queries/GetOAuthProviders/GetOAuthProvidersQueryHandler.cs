using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace AuthGate.Auth.Application.Features.Auth.Queries.GetOAuthProviders;

public class GetOAuthProvidersQueryHandler : IRequestHandler<GetOAuthProvidersQuery, OAuthProvidersResponseDto>
{
    private readonly IConfiguration _configuration;

    public GetOAuthProvidersQueryHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<OAuthProvidersResponseDto> Handle(GetOAuthProvidersQuery request, CancellationToken cancellationToken)
    {
        var googleClientId = _configuration["OAuth:Google:ClientId"];
        var facebookAppId = _configuration["OAuth:Facebook:AppId"];

        var response = new OAuthProvidersResponseDto
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

        return Task.FromResult(response);
    }
}
