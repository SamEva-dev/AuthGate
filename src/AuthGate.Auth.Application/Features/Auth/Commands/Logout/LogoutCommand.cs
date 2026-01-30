using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Logout;

public record LogoutCommand : IRequest<Result<bool>>;
