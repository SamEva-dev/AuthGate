using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Queries.GetFacebookConfig;

public record GetFacebookConfigQuery : IRequest<Result<OAuthConfigDto>>;
