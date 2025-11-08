using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Handler for DeleteUserCommand (soft delete)
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        UserManager<User> userManager,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            _logger.LogWarning("User not found for deletion: {UserId}", request.UserId);
            return Result.Failure<bool>("User not found");
        }

        // Soft delete: set IsActive to false
        user.IsActive = false;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to delete user {UserId}: {Errors}", request.UserId, errors);
            return Result.Failure<bool>($"Delete failed: {errors}");
        }

        _logger.LogInformation("User soft deleted: {UserId}", request.UserId);
        return Result.Success(true);
    }
}
