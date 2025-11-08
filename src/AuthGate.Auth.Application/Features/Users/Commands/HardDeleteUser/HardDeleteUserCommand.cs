using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users.Commands.HardDeleteUser;

public record HardDeleteUserCommand(Guid UserId) : IRequest<Result<bool>>;
