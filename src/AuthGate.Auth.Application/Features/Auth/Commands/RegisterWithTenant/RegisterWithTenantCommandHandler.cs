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
                return Result.Failure<RegisterWithTenantResponse>("Failed to create organization. Please try again.");
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<OrganizationDto>>(cancellationToken);
            
            if (apiResponse?.Value == null)
            {
                return Result.Failure<RegisterWithTenantResponse>("Invalid response from organization service");
            }

            var organization = apiResponse.Value;
            
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
                
                // TODO: Compensating transaction - delete organization if user creation fails
                // await DeleteOrganizationAsync(organization.OrganizationId);
                
                return Result.Failure<RegisterWithTenantResponse>($"Failed to create user: {errors}");
            }

            // 4. Assign TenantOwner role
            var roleResult = await _userManager.AddToRoleAsync(user, Domain.Constants.Roles.TenantOwner);
            
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Failed to assign TenantOwner role to user {UserId}", user.Id);
                // Continue anyway, role can be assigned later
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
            return Result.Failure<RegisterWithTenantResponse>($"Registration failed: {ex.Message}");
        }
    }

    // DTOs for LocaGuest API communication
    private record ApiResponse<T>
    {
        public bool IsSuccess { get; init; }
        public T? Value { get; init; }
        public string? Error { get; init; }
    }

    private record OrganizationDto
    {
        public Guid OrganizationId { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
