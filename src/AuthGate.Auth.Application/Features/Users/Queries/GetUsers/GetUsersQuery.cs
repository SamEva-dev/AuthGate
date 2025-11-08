using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Models;
using AuthGate.Auth.Application.DTOs.Users;
using MediatR;

namespace AuthGate.Auth.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Query to get paginated list of users
/// </summary>
public record GetUsersQuery : IRequest<Result<PagedResult<UserDto>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public string? Role { get; init; }
}
