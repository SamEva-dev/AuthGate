using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Users;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Query to get user details by ID
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDetailDto>>;
