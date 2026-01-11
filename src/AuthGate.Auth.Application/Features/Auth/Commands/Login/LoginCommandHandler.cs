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
    private readonly IMfaSecretRepository _mfaSecretRepository;
    private readonly ITrustedDeviceRepository _trustedDeviceRepository;
    private readonly IJwtService _jwtService;
    private readonly IUserRoleService _userRoleService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        IMfaSecretRepository mfaSecretRepository,
        ITrustedDeviceRepository trustedDeviceRepository,
        IJwtService jwtService,
        IUserRoleService userRoleService,
        ILogger<LoginCommandHandler> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _mfaSecretRepository = mfaSecretRepository;
        _trustedDeviceRepository = trustedDeviceRepository;
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

            // Check if 2FA/MFA is enabled for this user
            var mfaSecret = await _mfaSecretRepository.GetByUserIdAsync(user.Id, cancellationToken);
            if (mfaSecret?.IsEnabled == true)
            {
                // Check if device is trusted (skip 2FA if yes)
                if (!string.IsNullOrEmpty(request.DeviceFingerprint))
                {
                    var trustedDevice = await _trustedDeviceRepository
                        .GetByUserAndFingerprintAsync(user.Id, request.DeviceFingerprint, cancellationToken);
                    
                    if (trustedDevice?.IsValid() == true)
                    {
                        _logger.LogInformation("User {UserId} login from trusted device: {DeviceName}", user.Id, trustedDevice.DeviceName);
                        
                        // Update last used
                        trustedDevice.UpdateLastUsed();
                        _trustedDeviceRepository.Update(trustedDevice);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        // Skip 2FA! Generate tokens directly
                        var trustedRoles = await _userRoleService.GetUserRolesAsync(user);
                        var trustedPermissions = await _userRoleService.GetUserPermissionsAsync(user);

                        // Restrict access to Access-Manager-Pro to specific roles only
                        // Allowed: SuperAdmin, TenantOwner
                        if (!trustedRoles.Any(r => string.Equals(r, Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
                                               || string.Equals(r, Domain.Constants.Roles.TenantOwner, StringComparison.OrdinalIgnoreCase)))
                        {
                            _logger.LogWarning("Login denied for {UserId} ({Email}): role not allowed", user.Id, user.Email);
                            return Result.Failure<LoginResponseDto>("Access denied");
                        }

                        if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
                        {
                            return Result.Failure<LoginResponseDto>("Cannot issue access token without OrganizationId.");
                        }

                        var trustedAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, trustedRoles, trustedPermissions, true, user.OrganizationId.Value);
                        var trustedRefreshToken = _jwtService.GenerateRefreshToken();
                        var trustedJwtId = _jwtService.GetJwtId(trustedAccessToken) ?? Guid.NewGuid().ToString();
                        
                        var trustedRefreshTokenEntity = new Domain.Entities.RefreshToken
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            Token = trustedRefreshToken,
                            JwtId = trustedJwtId,
                            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                            CreatedAtUtc = DateTime.UtcNow
                        };
                        await _unitOfWork.RefreshTokens.AddAsync(trustedRefreshTokenEntity, cancellationToken);
                        
                        user.LastLoginAtUtc = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        _logger.LogInformation("User {UserId} logged in successfully (trusted device, 2FA skipped)", user.Id);
                        
                        return Result.Success(new LoginResponseDto
                        {
                            AccessToken = trustedAccessToken,
                            RefreshToken = trustedRefreshToken,
                            ExpiresIn = 900,
                            RequiresMfa = false
                        });
                    }
                }
                
                // Device not trusted or no fingerprint provided → Require 2FA
                // Generate temporary token for MFA verification (short-lived, 5 minutes)
                var mfaToken = Guid.NewGuid().ToString("N"); // Simple GUID for now
                
                // Store MFA token temporarily (TODO: Use cache/Redis in production)
                var mfaTokenEntity = new Domain.Entities.RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = mfaToken,
                    JwtId = $"mfa_{Guid.NewGuid():N}",
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5), // Short expiration for MFA
                    CreatedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.RefreshTokens.AddAsync(mfaTokenEntity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("User {UserId} requires 2FA verification", user.Id);
                return Result.Success(new LoginResponseDto
                {
                    RequiresMfa = true,
                    MfaToken = mfaToken
                });
            }

            // Get user roles and permissions using Identity
            _logger.LogDebug("Getting user roles and permissions for {UserId}", user.Id);
            var roles = await _userRoleService.GetUserRolesAsync(user);
            var permissions = await _userRoleService.GetUserPermissionsAsync(user);
            _logger.LogDebug("Found {RoleCount} roles and {PermissionCount} permissions", roles.Count(), permissions.Count());

            // Restrict access to Access-Manager-Pro to specific roles only
            // Allowed: SuperAdmin, TenantOwner
            if (!roles.Any(r => string.Equals(r, Domain.Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase)
                             || string.Equals(r, Domain.Constants.Roles.TenantOwner, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Login denied for {UserId} ({Email}): role not allowed", user.Id, user.Email);
                return Result.Failure<LoginResponseDto>("Access denied");
            }

            // Generate tokens (MFA is false here since we passed the check above)
            _logger.LogDebug("Generating access token");
            if (!user.OrganizationId.HasValue || user.OrganizationId.Value == Guid.Empty)
            {
                return Result.Failure<LoginResponseDto>("Cannot issue access token without OrganizationId.");
            }

            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email!, roles, permissions, false, user.OrganizationId.Value);
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
