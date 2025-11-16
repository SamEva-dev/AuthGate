using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;

namespace AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;

/// <summary>
/// Orchestrates the registration workflow:
/// 1. Call LocaGuest API to create Organization (Tenant)
/// 2. Create User in AuthGate with TenantId
/// 3. Assign TenantOwner role
/// 4. Generate JWT with tenant claims
/// </summary>
public class RegisterWithTenantCommandHandler : IRequestHandler<RegisterWithTenantCommand, Result<RegisterWithTenantResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RegisterWithTenantCommandHandler> _logger;

    public RegisterWithTenantCommandHandler(
        UserManager<User> userManager,
        ITokenService tokenService,
        IHttpClientFactory httpClientFactory,
        ILogger<RegisterWithTenantCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
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
            
            var httpClient = _httpClientFactory.CreateClient("LocaGuestApi");
            
            var createOrgRequest = new
            {
                name = request.OrganizationName,
                email = request.Email,
                phone = request.Phone
            };

            var response = await httpClient.PostAsJsonAsync("/api/organizations", createOrgRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to create organization: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                // COMPENSATING TRANSACTION: Rollback organization if it was created
                if (createdOrganizationId.HasValue)
                {
                    await RollbackOrganizationAsync(createdOrganizationId.Value, cancellationToken);
                }
                return Result.Failure<RegisterWithTenantResponse>("Failed to create organization. Please try again.");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>(cancellationToken);
            
            if (apiResponse?.Data == null)
            {
                // COMPENSATING TRANSACTION: Rollback organization if it was created
                if (createdOrganizationId.HasValue)
                {
                    await RollbackOrganizationAsync(createdOrganizationId.Value, cancellationToken);
                }
                return Result.Failure<RegisterWithTenantResponse>("Invalid response from organization service");
            }

            var organization = apiResponse.Data;
            createdOrganizationId = organization.OrganizationId; // Track for rollback
            
            _logger.LogInformation("Organization created: {Code} - {Name}", organization.Code, organization.Name);

            // 3. Create User in AuthGate with TenantId
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true, // Auto-confirm for now
                FirstName = request.FirstName,
                LastName = request.LastName,
                TenantId = organization.OrganizationId, // Link to LocaGuest Organization
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
                "User registered successfully: {UserId} - {Email} - Tenant: {TenantCode}",
                user.Id, user.Email, organization.Code);

            var responseDto = new RegisterWithTenantResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                TenantId = organization.OrganizationId,
                TenantCode = organization.Code,
                TenantName = organization.Name,
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
            
            var httpClient = _httpClientFactory.CreateClient("LocaGuestApi");
            var deleteResponse = await httpClient.DeleteAsync($"/api/organizations/{organizationId}", cancellationToken);
            
            if (deleteResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("ROLLBACK: Successfully deleted organization {OrganizationId}", organizationId);
            }
            else
            {
                var errorContent = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "ROLLBACK FAILED: Could not delete organization {OrganizationId}. Status: {StatusCode}, Error: {Error}. MANUAL CLEANUP REQUIRED!",
                    organizationId, deleteResponse.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "ROLLBACK EXCEPTION: Failed to delete organization {OrganizationId}. MANUAL CLEANUP REQUIRED!", 
                organizationId);
        }
    }

    // DTOs for LocaGuest API communication
    // ✅ Correspond à Result<T> de LocaGuest
    private record ApiResponse<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }  // ✅ Changed from Value to Data
        public string? ErrorMessage { get; init; }  // ✅ Changed from Error to ErrorMessage
    }

    private record OrganizationDto
    {
        public Guid OrganizationId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
