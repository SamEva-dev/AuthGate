using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Application.Common.Clients.Models;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;

/// <summary>
/// Orchestrates the registration workflow:
/// 1. Call LocaGuest API to create Organization (Tenant)
/// 2. Create User in AuthGate with OrganizationId
/// 3. Assign TenantOwner role
/// 4. Generate JWT with tenant claims
/// </summary>
public class RegisterWithTenantCommandHandler : IRequestHandler<RegisterWithTenantCommand, Result<RegisterWithTenantResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILocaGuestProvisioningClient _provisioningClient;
    private readonly ILogger<RegisterWithTenantCommandHandler> _logger;

    public RegisterWithTenantCommandHandler(
        UserManager<User> userManager,
        ITokenService tokenService,
        ILocaGuestProvisioningClient provisioningClient,
        ILogger<RegisterWithTenantCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _provisioningClient = provisioningClient;
        _logger = logger;
    }

    public async Task<Result<RegisterWithTenantResponse>> Handle(
        RegisterWithTenantCommand request,
        CancellationToken cancellationToken)
    {
        Guid? createdOrganizationId = null;
        
        try
        {
            // 1. Validate user doesn't already exist
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result.Failure<RegisterWithTenantResponse>($"User with email '{request.Email}' already exists");
            }

            // 2. Call LocaGuest API to create Organization (Tenant)
            _logger.LogInformation("Creating organization in LocaGuest for {Email}", request.Email);

            var userId = Guid.NewGuid();

            var orgRequest = new ProvisionOrganizationRequest
            {
                OrganizationName = request.OrganizationName,
                OrganizationEmail = request.Email,
                OrganizationPhone = request.Phone,
                OwnerUserId = userId.ToString("D"),
                OwnerEmail = request.Email
            };

            var provisioned = await _provisioningClient.ProvisionOrganizationAsync(orgRequest, cancellationToken);

            if (provisioned is null)
            {
                return Result.Failure<RegisterWithTenantResponse>("Failed to provision organization. Please try again.");
            }

            createdOrganizationId = provisioned.OrganizationId;

            _logger.LogInformation("Organization provisioned: {Code} - {Name}", provisioned.Code, provisioned.Name);

            // 3. Create User in AuthGate with OrganizationId
            var user = new User
            {
                Id = userId,
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true, // Auto-confirm for now
                FirstName = request.FirstName,
                LastName = request.LastName,
                OrganizationId = provisioned.OrganizationId, // Link to LocaGuest Organization
                IsActive = true,
                MfaEnabled = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user: {Errors}", errors);
                
                // COMPENSATING TRANSACTION: Delete organization since user creation failed
                await RollbackOrganizationAsync(createdOrganizationId.Value, cancellationToken);
                
                return Result.Failure<RegisterWithTenantResponse>($"Failed to create user: {errors}");
            }

            // 4. Assign TenantOwner role
            var roleResult = await _userManager.AddToRoleAsync(user, Domain.Constants.Roles.TenantOwner);

            if (!roleResult.Succeeded)
            {
                var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign TenantOwner role to user {UserId}: {Errors}", user.Id, roleErrors);
                
                // ROLLBACK: Delete user and organization
                await _userManager.DeleteAsync(user);
                await RollbackOrganizationAsync(createdOrganizationId.Value, cancellationToken);
                
                return Result.Failure<RegisterWithTenantResponse>($"Failed to assign role: {roleErrors}");
            }

            // 5. Generate JWT with tenant claims
            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);

            _logger.LogInformation(
                "User registered successfully: {UserId} - {Email} - Organization: {OrganizationCode}",
                user.Id, user.Email, provisioned.Code);

            var responseDto = new RegisterWithTenantResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                OrganizationId = provisioned.OrganizationId,
                OrganizationCode = provisioned.Code,
                OrganizationName = provisioned.Name,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = Domain.Constants.Roles.TenantOwner
            };

            return Result<RegisterWithTenantResponse>.Success(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration workflow for {Email}", request.Email);
            
            // COMPENSATING TRANSACTION: Rollback organization if it was created
            if (createdOrganizationId.HasValue)
            {
                await RollbackOrganizationAsync(createdOrganizationId.Value, cancellationToken);
            }
            
            return Result.Failure<RegisterWithTenantResponse>($"Registration failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Compensating transaction: Delete organization in LocaGuest if user creation fails
    /// Implements the Saga pattern for distributed transaction rollback
    /// </summary>
    private async Task RollbackOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("ROLLBACK: Attempting to delete organization {OrganizationId} due to user creation failure", organizationId);

            var deleted = await _provisioningClient.HardDeleteOrganizationAsync(organizationId, cancellationToken);

            if (deleted)
            {
                _logger.LogInformation("ROLLBACK: Successfully deleted organization {OrganizationId}", organizationId);
            }
            else
            {
                _logger.LogError(
                    "ROLLBACK FAILED: Could not delete organization {OrganizationId}. MANUAL CLEANUP REQUIRED!",
                    organizationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "ROLLBACK EXCEPTION: Failed to delete organization {OrganizationId}. MANUAL CLEANUP REQUIRED!", 
                organizationId);
        }
    }
}
