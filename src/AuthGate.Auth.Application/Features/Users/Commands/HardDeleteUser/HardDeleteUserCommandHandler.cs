using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Users.Commands.HardDeleteUser;

public class HardDeleteUserCommandHandler : IRequestHandler<HardDeleteUserCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<HardDeleteUserCommandHandler> _logger;

    public HardDeleteUserCommandHandler(UserManager<User> userManager, ILogger<HardDeleteUserCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(HardDeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found for hard delete: {UserId}", request.UserId);
            return Result.Failure<bool>("User not found");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to hard delete user {UserId}: {Errors}", request.UserId, errors);
            return Result.Failure<bool>($"Hard delete failed: {errors}");
        }

        _logger.LogInformation("User hard deleted: {UserId}", request.UserId);
        return Result.Success(true);
    }
}
