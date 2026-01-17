using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace AuthGate.Auth.Application.Features.Auth.Queries.GetFacebookConfig;

public class GetFacebookConfigQueryHandler : IRequestHandler<GetFacebookConfigQuery, Result<OAuthConfigDto>>
{
    private readonly IConfiguration _configuration;

    public GetFacebookConfigQueryHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Result<OAuthConfigDto>> Handle(GetFacebookConfigQuery request, CancellationToken cancellationToken)
    {
        var appId = _configuration["OAuth:Facebook:AppId"];

        if (string.IsNullOrEmpty(appId))
        {
            return Task.FromResult(Result.Failure<OAuthConfigDto>("Facebook OAuth is not configured"));
        }

        var config = new OAuthConfigDto
        {
            AppId = appId,
            Scope = "email,public_profile"
        };

        return Task.FromResult(Result.Success(config));
    }
}
