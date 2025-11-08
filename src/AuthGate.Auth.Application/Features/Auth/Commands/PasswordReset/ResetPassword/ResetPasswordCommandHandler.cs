using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.PasswordReset.ResetPassword;

/// <summary>
/// Handler for ResetPasswordCommand
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly DbContext _context;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        UserManager<User> userManager,
        DbContext context,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            _logger.LogWarning("Password reset attempted for non-existent email: {Email}", request.Email);
            return Result.Failure<bool>("Invalid reset token or email");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset attempted for inactive user: {Email}", request.Email);
            return Result.Failure<bool>("User account is not active");
        }

        // Verify token exists in database and is not expired
        var storedToken = await _context.Set<PasswordResetToken>()
            .Where(t => t.UserId == user.Id && t.Token == request.Token && !t.IsUsed)
            .OrderByDescending(t => t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Invalid or already used reset token for user: {Email}", request.Email);
            return Result.Failure<bool>("Invalid or expired reset token");
        }

        if (storedToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired reset token used for user: {Email}", request.Email);
            return Result.Failure<bool>("Reset token has expired. Please request a new one");
        }

        // Reset password using Identity (validates token)
        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Password reset failed for user {Email}: {Errors}", request.Email, errors);
            return Result.Failure<bool>($"Password reset failed: {errors}");
        }

        // Mark token as used
        storedToken.IsUsed = true;
        storedToken.UsedAtUtc = DateTime.UtcNow;
        _context.Set<PasswordResetToken>().Update(storedToken);

        // Invalidate all refresh tokens for security
        var refreshTokens = await _context.Set<Domain.Entities.RefreshToken>()
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully reset for user: {Email}", request.Email);

        return Result.Success(true);
    }
}
