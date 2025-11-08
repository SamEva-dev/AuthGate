using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.DTOs.Permissions;
using MediatR;

namespace AuthGate.Auth.Application.Features.Permissions.Queries.GetPermissions;

public record GetPermissionsQuery : IRequest<Result<List<PermissionDto>>>;
