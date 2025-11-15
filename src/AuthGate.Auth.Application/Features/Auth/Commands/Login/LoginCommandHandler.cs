using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handler for LoginCommand - Uses SignInManager for authentication
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        ILogger<LoginCommandHandler> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _userRoleService = userRoleService;
        _logger = logger;
    }

    public async Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Login attempt for {Email}", request.Email);
            
            // Get user using UserManager
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
                return Result.Failure<LoginResponseDto>("Invalid email or password");
            }
            
            _logger.LogDebug("User found: {UserId}", user.Id);

            // Check if user is active (custom business rule)
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {UserId}", user.Id);
                return Result.Failure<LoginResponseDto>("Account is inactive");
            }

            // Use SignInManager to check password and handle lockout automatically
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login attempt for locked user: {UserId}", user.Id);
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                return Result.Failure<LoginResponseDto>($"Account is locked until {lockoutEnd?.UtcDateTime:g} UTC");
            }

            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid password for user: {UserId}", user.Id);
                return Result.Failure<LoginResponseDto>("Invalid email or password");
            }

            // If MFA is enabled, return temporary token for MFA verification
            if (user.MfaEnabled)
            {
                var mfaToken = _jwtService.GenerateRefreshToken(); // Use as temporary token
                var response = new LoginResponseDto
                {
                    RequiresMfa = true,
                    MfaToken = mfaToken
                };

                _logger.LogInformation("User {UserId} requires MFA verification", user.Id);
                return Result.Success(response);
            }

            // Get user roles and permissions using Identity
            _logger.LogDebug("Getting user roles and permissions for {UserId}", user.Id);
            var roles = await _userRoleService.GetUserRolesAsync(user);
            var permissions = await _userRoleService.GetUserPermissionsAsync(user);
            _logger.LogDebug("Found {RoleCount} roles and {PermissionCount} permissions", roles.Count(), permissions.Count());

            // Generate tokens
            _logger.LogDebug("Generating access token");
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles, permissions, user.MfaEnabled, user.TenantId);
            _logger.LogDebug("Generating refresh token");
            var refreshToken = _jwtService.GenerateRefreshToken();
            _logger.LogDebug("Extracting JWT ID");
            var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();
            _logger.LogDebug("JWT ID: {JwtId}", jwtId);

            // Store refresh token (using repository for custom entity)
            var refreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                JwtId = jwtId,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow
            };

            _logger.LogDebug("Saving refresh token to database");
            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

            // Update last login using UserManager
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogDebug("Saving changes to database");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);

            return Result.Success(new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                RequiresMfa = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}: {Message}", request.Email, ex.Message);
            throw;
        }
    }
}
