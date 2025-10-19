
using AuthGate.Auth.Application.DTOs;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users;

public record ListUsersQuery(Guid UserId,  string Ip, string Agent) : IRequest<IEnumerable<UserDto>>
{
}
