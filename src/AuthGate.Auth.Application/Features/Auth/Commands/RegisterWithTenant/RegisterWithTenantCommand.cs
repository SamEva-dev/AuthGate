using AuthGate.Auth.Application.Common;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;

/// <summary>
/// Command to register a new TenantOwner with automatic organization creation
/// This is the entry point for new customers/agencies/owners
/// </summary>
public record RegisterWithTenantCommand : IRequest<Result<RegisterWithTenantResponse>>
{
    /// <summary>
    /// User's email (will be organization email too)
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// User's password
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Organization/Agency name
    /// </summary>
    public string OrganizationName { get; init; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Optional phone
    /// </summary>
    public string? Phone { get; init; }
}

public record RegisterWithTenantResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string TenantCode { get; init; } = string.Empty;
    public string TenantName { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
