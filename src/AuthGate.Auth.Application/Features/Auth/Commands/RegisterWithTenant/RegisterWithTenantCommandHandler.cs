using System.Text.Json;
using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using Roles = AuthGate.Auth.Domain.Constants.Roles;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using AuthGate.Auth.Domain.Repositories;
using LocaGuest.Emailing.Abstractions;
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
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailingService _emailing;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RegisterWithTenantCommandHandler> _logger;

    public RegisterWithTenantCommandHandler(
        UserManager<User> userManager,
        ITokenService tokenService,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IEmailingService emailing,
        IConfiguration configuration,
        ILogger<RegisterWithTenantCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _emailing = emailing;
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
                // Check if account was deactivated (soft deleted)
                if (!existingUser.IsActive && existingUser.DeactivatedAtUtc.HasValue)
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
                
                return Result.Failure<RegisterWithTenantResponse>($"User with email '{request.Email}' already exists");
            }

            var userId = Guid.NewGuid();

            // 2. Create User in AuthGate with PendingProvisioning status
            var user = new User
            {
                Id = userId,
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = false,
                FirstName = request.FirstName,
                LastName = request.LastName,
                OrganizationId = null, // Will be set after async provisioning
                IsActive = false,
                Status = UserStatus.PendingProvisioning,
                MfaEnabled = false,
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

            // 4. Generate JWT BEFORE saving outbox (use pending provisioning tokens)
            var (accessToken, refreshToken) = await _tokenService.GeneratePendingProvisioningTokensAsync(user);

            // 4b. Send email verification link
            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var frontendUrl = _configuration["Frontend:ConfirmEmailUrl"] ?? "http://localhost:4200/confirm-email";
            var verifyUrl = $"{frontendUrl}?token={Uri.EscapeDataString(confirmToken)}&email={Uri.EscapeDataString(user.Email!)}";
            var subject = "Vérifiez votre adresse email";
            var firstName = user.FirstName ?? string.Empty;
            var htmlBody = $$"""
<h2>✉️ Vérification d'email</h2>
<p>Bonjour {{firstName}},</p>
<p>Pour finaliser votre inscription, veuillez vérifier votre adresse email en cliquant sur le bouton ci-dessous :</p>
<p><a href="{{verifyUrl}}">Vérifier mon email</a></p>
<p>Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.</p>
""";

            await _emailing.QueueHtmlAsync(
                toEmail: user.Email!,
                subject: subject,
                htmlContent: htmlBody,
                textContent: null,
                attachments: null,
                tags: EmailUseCaseTags.AuthConfirmEmail,
                cancellationToken: cancellationToken);

            // 5. Create OutboxMessage for async organization provisioning
            var payload = new ProvisionOrganizationPayload
            {
                UserId = userId,
                Email = request.Email,
                OrganizationName = request.OrganizationName,
                Phone = request.Phone,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var outboxMessage = OutboxMessage.Create(
                OutboxMessageType.ProvisionOrganization,
                JsonSerializer.Serialize(payload),
                userId,
                Guid.NewGuid().ToString("N") // CorrelationId for tracing
            );

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "User created with pending provisioning: {UserId} - {Email}. OutboxMessage: {OutboxId}",
                user.Id, user.Email, outboxMessage.Id);

            var responseDto = new RegisterWithTenantResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                OrganizationId = null, // Not yet provisioned
                OrganizationCode = null,
                OrganizationName = request.OrganizationName, // Show requested name
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Role = AuthGate.Auth.Domain.Constants.Roles.TenantOwner,
                Status = "pending_provisioning",
                Message = "Your account is being set up. You'll have full access shortly."
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
