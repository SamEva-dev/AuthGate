using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace AuthGate.Auth.Application.Features.Auth.Queries.GetGoogleConfig;

public class GetGoogleConfigQueryHandler : IRequestHandler<GetGoogleConfigQuery, Result<OAuthConfigDto>>
{
    private readonly IConfiguration _configuration;

    public GetGoogleConfigQueryHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Result<OAuthConfigDto>> Handle(GetGoogleConfigQuery request, CancellationToken cancellationToken)
    {
        var clientId = _configuration["OAuth:Google:ClientId"];

        if (string.IsNullOrEmpty(clientId))
        {
            return Task.FromResult(Result.Failure<OAuthConfigDto>("Google OAuth is not configured"));
        }

        var config = new OAuthConfigDto
        {
            ClientId = clientId,
            Scope = "openid email profile"
        };

        return Task.FromResult(Result.Success(config));
    }
}
