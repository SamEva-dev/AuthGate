using MediatR;

namespace AuthGate.Auth.Application.Features.TwoFactor.Queries.GetTwoFactorStatus;

/// <summary>
/// Query to get 2FA status for the current user
/// </summary>
public class GetTwoFactorStatusQuery : IRequest<TwoFactorStatusDto>
{
    // Uses current authenticated user from context
}

/// <summary>
/// DTO containing 2FA status information
/// </summary>
public class TwoFactorStatusDto
{
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int RecoveryCodesRemaining { get; set; }
}
