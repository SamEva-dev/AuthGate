using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.InviteCollaborator;

/// <summary>
/// Command to invite a collaborator to join an organization
/// Only TenantOwner and TenantAdmin can invite users
/// </summary>
public record InviteCollaboratorCommand : IRequest<Result<InviteCollaboratorResponse>>
{
    /// <summary>
    /// Email of the person to invite
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Role to assign (TenantAdmin, TenantManager, TenantUser, ReadOnly)
    /// Cannot be SuperAdmin or TenantOwner
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Optional personal message
    /// </summary>
    public string? Message { get; init; }
}

public record InviteCollaboratorResponse
{
    public Guid InvitationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string InvitationUrl { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
