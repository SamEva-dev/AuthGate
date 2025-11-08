using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users.Commands.ReactivateUser;

public record ReactivateUserCommand(Guid UserId) : IRequest<Result<bool>>;
