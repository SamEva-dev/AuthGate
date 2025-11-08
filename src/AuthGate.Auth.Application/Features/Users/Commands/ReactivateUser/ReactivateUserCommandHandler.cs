using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Commands.ReactivateUser;

public class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ReactivateUserCommandHandler> _logger;

    public ReactivateUserCommandHandler(UserManager<User> userManager, ILogger<ReactivateUserCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found for reactivation: {UserId}", request.UserId);
            return Result.Failure<bool>("User not found");
        }

        user.IsActive = true;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to reactivate user {UserId}: {Errors}", request.UserId, errors);
            return Result.Failure<bool>($"Reactivate failed: {errors}");
        }

        _logger.LogInformation("User reactivated: {UserId}", request.UserId);
        return Result.Success(true);
    }
}
