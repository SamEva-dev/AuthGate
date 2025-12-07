using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AuthGate.Auth.Application.Features.TwoFactor.Queries.GetTwoFactorStatus;

/// <summary>
/// Handler for getting 2FA status
/// </summary>
public class GetTwoFactorStatusQueryHandler : IRequestHandler<GetTwoFactorStatusQuery, TwoFactorStatusDto>
{
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetTwoFactorStatusQueryHandler(
        IMfaSecretRepository mfaSecretRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _mfaSecretRepository = mfaSecretRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TwoFactorStatusDto> Handle(GetTwoFactorStatusQuery request, CancellationToken cancellationToken)
    {
        // Get current user ID from JWT
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(userId, cancellationToken);
        
        if (mfaSecret == null)
        {
            // 2FA not configured
            return new TwoFactorStatusDto
            {
                IsEnabled = false,
                EnabledAt = null,
                LastUsedAt = null,
                RecoveryCodesRemaining = 0
            };
        }

        return new TwoFactorStatusDto
        {
            IsEnabled = mfaSecret.IsEnabled,
            EnabledAt = mfaSecret.EnabledAtUtc,
            LastUsedAt = mfaSecret.LastUsedAtUtc,
            RecoveryCodesRemaining = mfaSecret.RecoveryCodesRemaining
        };
    }
}
