using AuthGate.Auth.Application.DTOs.Auth;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Queries.GetOAuthProviders;

public record GetOAuthProvidersQuery : IRequest<OAuthProvidersResponseDto>;
