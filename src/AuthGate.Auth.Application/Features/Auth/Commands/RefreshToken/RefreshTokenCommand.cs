using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Enums;
using MediatR;

namespace AuthGate.Auth.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an access token using a refresh token
/// </summary>
public record RefreshTokenCommand : IRequest<Result<TokenResponseDto>>, IAuditableCommand
{
    /// <summary>
    /// Refresh token
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Current access token (optional) used to preserve app + org context during refresh
    /// </summary>
    public string? AccessToken { get; init; }

    public AuditAction AuditAction => AuditAction.TokenRefreshed;

    public string GetAuditDescription() => "Token refresh request";
}
