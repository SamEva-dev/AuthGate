using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Roles;
using MediatR;

namespace AuthGate.Auth.Application.Features.Roles.Queries.GetRoles;

public record GetRolesQuery : IRequest<Result<List<RoleDto>>>;
