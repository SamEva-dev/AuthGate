using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Application.Common.Clients.Models;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using Roles = AuthGate.Auth.Domain.Constants.Roles;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.RegisterWithTenant;

/// <summary>
/// Orchestrates the registration workflow using the Outbox Pattern for resilience:
/// 1. Create User in AuthGate with Status = PendingProvisioning
/// 2. Add OutboxMessage for async organization provisioning
/// 3. Assign TenantOwner role
/// 4. Generate JWT (limited until provisioning completes)
/// 5. Return immediately - provisioning happens async via OutboxProcessor
/// </summary>
public class RegisterWithTenantCommandHandler : IRequestHandler<RegisterWithTenantCommand, Result<RegisterWithTenantResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILocaGuestProvisioningClient _provisioningClient;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterWithTenantCommandHandler> _logger;

    public RegisterWithTenantCommandHandler(
        UserManager<User> userManager,
        ITokenService tokenService,
        ILocaGuestProvisioningClient provisioningClient,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<RegisterWithTenantCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _provisioningClient = provisioningClient;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<RegisterWithTenantResponse>> Handle(
        RegisterWithTenantCommand request,
        CancellationToken cancellationToken)
    {
        User? createdUser = null;
        
        try
        {
            // 1. Check if user already exists
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
                        return Result.Failure<RegisterWithTenantResponse>($"User with email '{request.Email}' already exists");
                    }

                    existingUser = null;
                }

                if (existingUser != null && !existingUser.EmailConfirmed)
                {
                    var resendOutboxMessage = OutboxMessage.Create(
                        OutboxMessageType.SendConfirmEmail,
                        "{}",
                        existingUser.Id,
                        Guid.NewGuid().ToString("N")
                    );

                    await _outboxRepository.AddAsync(resendOutboxMessage, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    return Result<RegisterWithTenantResponse>.Success(new RegisterWithTenantResponse
                    {
                        UserId = existingUser.Id,
                        Email = existingUser.Email ?? request.Email,
                        OrganizationId = existingUser.OrganizationId,
                        OrganizationCode = null,
                        OrganizationName = request.OrganizationName,
                        AccessToken = string.Empty,
                        RefreshToken = string.Empty,
                        Role = AuthGate.Auth.Domain.Constants.Roles.TenantOwner,
                        Status = "pending_email_confirmation",
                        Message = "Veuillez confirmer votre adresse email pour finaliser votre inscription."
                    });
                }
                else if (!existingUser.IsActive && existingUser.DeactivatedAtUtc.HasValue)
                {
                    // Reactivate the account
                    _logger.LogInformation("Reactivating previously deactivated account for {Email}", request.Email);
                    
                    existingUser.IsActive = true;
                    existingUser.DeactivatedAtUtc = null;
                    existingUser.Status = UserStatus.Active;
                    existingUser.UpdatedAtUtc = DateTime.UtcNow;
                    
                    // Update password if provided
                    if (!string.IsNullOrEmpty(request.Password))
                    {
                        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                        await _userManager.ResetPasswordAsync(existingUser, resetToken, request.Password);
                    }
                    
                    await _userManager.UpdateAsync(existingUser);
                    
                    // Generate tokens for returning user
                    var userRoles = await _userManager.GetRolesAsync(existingUser);
                    var tokens = await _tokenService.GenerateTokensAsync(existingUser);
                    
                    return Result<RegisterWithTenantResponse>.Success(new RegisterWithTenantResponse
                    {
                        UserId = existingUser.Id,
                        Email = existingUser.Email!,
                        OrganizationId = existingUser.OrganizationId,
                        OrganizationCode = null,
                        OrganizationName = request.OrganizationName,
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken,
                        Role = userRoles.FirstOrDefault() ?? AuthGate.Auth.Domain.Constants.Roles.TenantOwner,
                        Status = "reactivated",
                        Message = $"Heureux de vous revoir, {existingUser.FirstName ?? ""}! Votre compte a été réactivé."
                    });
                }
                
                if (existingUser != null)
                {
                    return Result.Failure<RegisterWithTenantResponse>($"User with email '{request.Email}' already exists");
                }
            }

            var requireEmailConfirmation = _configuration.GetValue<bool?>("Auth:RequireEmailConfirmation") ?? true;

            var userId = Guid.NewGuid();

            // 2. Create User in AuthGate
            var user = new User
            {
                Id = userId,
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = !requireEmailConfirmation,
                FirstName = request.FirstName,
                LastName = request.LastName,
                OrganizationId = null, // Will be set after async provisioning
                IsActive = !requireEmailConfirmation,
                Status = requireEmailConfirmation ? UserStatus.PendingEmailConfirmation : UserStatus.PendingProvisioning,
                MfaEnabled = false,
                PendingOrganizationName = request.OrganizationName,
                PendingOrganizationPhone = request.Phone,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user: {Errors}", errors);
                return Result.Failure<RegisterWithTenantResponse>($"Failed to create user: {errors}");
            }

            createdUser = user; // Track for rollback

            // 3. Assign TenantOwner role
            var roleResult = await _userManager.AddToRoleAsync(user, AuthGate.Auth.Domain.Constants.Roles.TenantOwner);

            if (!roleResult.Succeeded)
            {
                var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign TenantOwner role to user {UserId}: {Errors}", user.Id, roleErrors);
                
                // Rollback user creation
                await RollbackUserAsync(createdUser);
                return Result.Failure<RegisterWithTenantResponse>($"Failed to assign role: {roleErrors}");
            }

            if (!requireEmailConfirmation)
            {
                var orgRequest = new ProvisionOrganizationRequest
                {
                    OrganizationName = request.OrganizationName,
                    OrganizationEmail = request.Email,
                    OrganizationPhone = request.Phone,
                    OwnerUserId = user.Id.ToString("D"),
                    OwnerEmail = request.Email
                };

                var provisioned = await _provisioningClient.ProvisionOrganizationAsync(orgRequest, cancellationToken);
                if (provisioned == null)
                {
                    await RollbackUserAsync(createdUser);
                    return Result.Failure<RegisterWithTenantResponse>("Registration failed. Please try again.");
                }

                user.OrganizationId = provisioned.OrganizationId;
                user.IsActive = true;
                user.Status = UserStatus.Active;
                user.PendingOrganizationName = null;
                user.PendingOrganizationPhone = null;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    await RollbackUserAsync(createdUser);
                    return Result.Failure<RegisterWithTenantResponse>($"Failed to update user: {errors}");
                }

                var tokens = await _tokenService.GenerateTokensAsync(user);
                var userRoles = await _userManager.GetRolesAsync(user);

                return Result<RegisterWithTenantResponse>.Success(new RegisterWithTenantResponse
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    OrganizationId = user.OrganizationId,
                    OrganizationCode = provisioned.Code,
                    OrganizationName = provisioned.Name,
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken,
                    Role = userRoles.FirstOrDefault() ?? AuthGate.Auth.Domain.Constants.Roles.TenantOwner,
                    Status = "active",
                    Message = "Votre compte a été créé."
                });
            }

            // 4. Create OutboxMessage for async confirmation email sending
            var outboxMessage = OutboxMessage.Create(
                OutboxMessageType.SendConfirmEmail,
                "{}",
                userId,
                Guid.NewGuid().ToString("N") // CorrelationId for tracing
            );

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "User created with pending email confirmation: {UserId} - {Email}. OutboxMessage: {OutboxId}",
                user.Id, user.Email, outboxMessage.Id);

            var responseDto = new RegisterWithTenantResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                OrganizationId = null, // Not yet provisioned
                OrganizationCode = null,
                OrganizationName = request.OrganizationName, // Show requested name
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                Role = AuthGate.Auth.Domain.Constants.Roles.TenantOwner,
                Status = "pending_email_confirmation",
                Message = "Veuillez confirmer votre adresse email pour finaliser votre inscription."
            };

            return Result<RegisterWithTenantResponse>.Success(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            
            // Rollback user if created
            if (createdUser != null)
            {
                await RollbackUserAsync(createdUser);
            }
            
            return Result.Failure<RegisterWithTenantResponse>("Registration failed. Please try again.");
        }
    }

    private bool IsUnconfirmedUserExpired(User user)
    {
        var ttlMinutes = _configuration.GetValue<int?>("Auth:UnconfirmedAccountTtlMinutes")
            ?? ((_configuration.GetValue<int?>("Auth:UnconfirmedAccountTtlHours") ?? 0) * 60);

        if (ttlMinutes <= 0)
            return false;

        return user.CreatedAtUtc.AddMinutes(ttlMinutes) < DateTime.UtcNow;
    }

    private async Task RollbackUserAsync(User user)
    {
        try
        {
            _logger.LogWarning("ROLLBACK: Deleting user {UserId} due to registration failure", user.Id);
            await _userManager.DeleteAsync(user);
            _logger.LogInformation("ROLLBACK: User {UserId} deleted successfully", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ROLLBACK FAILED: Could not delete user {UserId}. MANUAL CLEANUP REQUIRED!", user.Id);
        }
    }
}
