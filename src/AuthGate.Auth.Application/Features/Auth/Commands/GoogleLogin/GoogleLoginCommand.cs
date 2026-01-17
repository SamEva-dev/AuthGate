using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.GoogleLogin;

public class GoogleLoginCommand : IRequest<Result<ExternalLoginResponseDto>>
{
    public string Token { get; set; } = string.Empty;
}
