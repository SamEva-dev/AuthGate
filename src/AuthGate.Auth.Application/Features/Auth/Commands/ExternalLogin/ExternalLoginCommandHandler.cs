using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.DTOs.Auth;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.ExternalLogin;

/// <summary>
/// Handler for ExternalLoginCommand - Authenticates users via external OAuth providers
/// </summary>
public class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, Result<ExternalLoginResponseDto>>
{
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<ExternalLoginCommandHandler> _logger;

    public ExternalLoginCommandHandler(
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        ILogger<ExternalLoginCommandHandler> logger)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _userRoleService = userRoleService;
        _logger = logger;
    }

    public async Task<Result<ExternalLoginResponseDto>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("External login attempt via {Provider} for {Email}", request.Provider, request.Email);

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Result.Failure<ExternalLoginResponseDto>("Email is required from OAuth provider");
            }

            // Check if user exists
            var user = await _userManager.FindByEmailAsync(request.Email);
            var isNewUser = false;

            if (user == null)
            {
                // Create new user from OAuth data
                _logger.LogInformation("Creating new user from {Provider} OAuth: {Email}", request.Provider, request.Email);
                
                user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName ?? "",
                    LastName = request.LastName ?? "",
                    EmailConfirmed = true, // OAuth emails are verified by the provider
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    ExternalProvider = request.Provider.ToLowerInvariant(),
                    ExternalProviderId = request.ProviderId,
                    ProfilePictureUrl = request.PictureUrl
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create user from OAuth: {Errors}", errors);
                    return Result.Failure<ExternalLoginResponseDto>($"Failed to create account: {errors}");
                }

                isNewUser = true;
                _logger.LogInformation("Created new user {UserId} from {Provider}", user.Id, request.Provider);
            }
            else
            {
                // Update external provider info if not set
                if (string.IsNullOrEmpty(user.ExternalProvider))
                {
                    user.ExternalProvider = request.Provider.ToLowerInvariant();
                    user.ExternalProviderId = request.ProviderId;
                    if (!string.IsNullOrEmpty(request.PictureUrl) && string.IsNullOrEmpty(user.ProfilePictureUrl))
                    {
                        user.ProfilePictureUrl = request.PictureUrl;
                    }
                    await _userManager.UpdateAsync(user);
                }
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("External login attempt for inactive user: {UserId}", user.Id);
                return Result.Failure<ExternalLoginResponseDto>("Account is inactive");
            }

            // Check if user has an organization
            if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
            {
                _logger.LogInformation("User {UserId} needs to complete registration (no organization)", user.Id);
                return Result.Success(new ExternalLoginResponseDto
                {
                    Success = true,
                    IsNewUser = isNewUser,
                    RequiresRegistration = true,
                    User = new ExternalUserInfoDto
                    {
                        Id = user.Id,
                        Email = user.Email ?? "",
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        PictureUrl = user.ProfilePictureUrl,
                        Provider = request.Provider,
                        ProviderId = request.ProviderId ?? ""
                    }
                });
            }

            // Get user roles and permissions
            var roles = await _userRoleService.GetUserRolesAsync(user);
            var permissions = await _userRoleService.GetUserPermissionsAsync(user);

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(
                user.Id, 
                user.Email!, 
                roles, 
                permissions, 
                false, 
                user.OrganizationId.Value);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var jwtId = _jwtService.GetJwtId(accessToken) ?? Guid.NewGuid().ToString();

            // Store refresh token
            var refreshTokenEntity = new Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = refreshToken,
                JwtId = jwtId,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

            // Update last login
            user.LastLoginAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} logged in successfully via {Provider}", user.Id, request.Provider);

            return Result.Success(new ExternalLoginResponseDto
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsNewUser = isNewUser,
                RequiresRegistration = false,
                User = new ExternalUserInfoDto
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FirstName = user.FirstName ?? "",
                    LastName = user.LastName ?? "",
                    PictureUrl = user.ProfilePictureUrl,
                    Provider = request.Provider,
                    ProviderId = request.ProviderId ?? "",
                    OrganizationId = user.OrganizationId,
                    Roles = roles.ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during external login for {Email}: {Message}", request.Email, ex.Message);
            return Result.Failure<ExternalLoginResponseDto>($"External login failed: {ex.Message}");
        }
    }
}
