using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.FacebookLogin;

public class FacebookLoginCommand : IRequest<Result<ExternalLoginResponseDto>>
{
    public string Token { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
}
