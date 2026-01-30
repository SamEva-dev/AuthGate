using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Register;

/// <summary>
/// Handler for RegisterCommand
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponseDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger<RegisterCommandHandler> logger)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<RegisterResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            if (!existingUser.EmailConfirmed && IsUnconfirmedUserExpired(existingUser))
            {
                _logger.LogWarning(
                    "Deleting expired unconfirmed user {UserId} for email {Email} to allow re-registration",
                    existingUser.Id,
                    request.Email);

                var deleteResult = await _userManager.DeleteAsync(existingUser);
                if (!deleteResult.Succeeded)
                {
                    var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                    _logger.LogWarning(
                        "Failed to delete expired unconfirmed user for {Email}: {Errors}",
                        request.Email,
                        errors);
                    return Result.Failure<RegisterResponseDto>("A user with this email already exists");
                }

                existingUser = null;
            }

            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                return Result.Failure<RegisterResponseDto>("A user with this email already exists");
            }
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            EmailConfirmed = false // Set to false if you want email confirmation
        };

        // Create user with password using Identity
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User registration failed for {Email}: {Errors}", request.Email, errors);
            return Result.Failure<RegisterResponseDto>($"Registration failed: {errors}");
        }

        _logger.LogInformation("User registered successfully: {UserId} - {Email}", user.Id, user.Email);

        var response = new RegisterResponseDto
        {
            UserId = user.Id,
            Email = user.Email!,
            Message = "Registration successful. You can now log in."
        };

        return Result.Success(response);
    }

    private bool IsUnconfirmedUserExpired(User user)
    {
        var ttlHours = _configuration.GetValue<int?>("Auth:UnconfirmedAccountTtlHours") ?? 0;
        if (ttlHours <= 0)
            return false;

        return user.CreatedAtUtc.AddHours(ttlHours) < DateTime.UtcNow;
    }
}
