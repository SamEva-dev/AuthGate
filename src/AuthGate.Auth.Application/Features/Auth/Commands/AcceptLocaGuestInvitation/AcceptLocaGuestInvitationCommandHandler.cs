using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Clients;
using AuthGate.Auth.Application.Common.Clients.Models;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.AcceptLocaGuestInvitation;

public sealed class AcceptLocaGuestInvitationCommandHandler : IRequestHandler<AcceptLocaGuestInvitationCommand, Result<AcceptLocaGuestInvitationResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILocaGuestInvitationProvisioningClient _locaGuest;
    private readonly ILogger<AcceptLocaGuestInvitationCommandHandler> _logger;

    public AcceptLocaGuestInvitationCommandHandler(
        UserManager<User> userManager,
        ITokenService tokenService,
        ILocaGuestInvitationProvisioningClient locaGuest,
        ILogger<AcceptLocaGuestInvitationCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _locaGuest = locaGuest;
        _logger = logger;
    }

    public async Task<Result<AcceptLocaGuestInvitationResponse>> Handle(AcceptLocaGuestInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return Result.Failure<AcceptLocaGuestInvitationResponse>("Email is required");
            if (string.IsNullOrWhiteSpace(request.Password))
                return Result.Failure<AcceptLocaGuestInvitationResponse>("Password is required");
            if (string.IsNullOrWhiteSpace(request.Token))
                return Result.Failure<AcceptLocaGuestInvitationResponse>("Token is required");

            var email = request.Email.Trim().ToLowerInvariant();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    IsActive = true,
                    MfaEnabled = false,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return Result.Failure<AcceptLocaGuestInvitationResponse>($"Failed to create user: {errors}");
                }
            }
            else
            {
                var ok = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!ok)
                    return Result.Failure<AcceptLocaGuestInvitationResponse>("Invalid credentials");

                if (user.OrganizationId.HasValue && user.OrganizationId.Value != Guid.Empty)
                    return Result.Failure<AcceptLocaGuestInvitationResponse>("User already belongs to an organization");
            }

            var consumed = await _locaGuest.ConsumeInvitationAsync(new ConsumeInvitationRequest
            {
                Token = request.Token,
                UserId = user.Id.ToString("D"),
                UserEmail = user.Email ?? email
            }, cancellationToken);

            if (consumed == null)
                return Result.Failure<AcceptLocaGuestInvitationResponse>("Failed to consume invitation");

            user.OrganizationId = consumed.OrganizationId;
            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
            {
                var errors = string.Join(", ", updateRes.Errors.Select(e => e.Description));
                return Result.Failure<AcceptLocaGuestInvitationResponse>($"Failed to update user organization: {errors}");
            }

            await _userManager.AddToRoleAsync(user, AuthGate.Auth.Domain.Constants.Roles.Occupant);

            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);

            _logger.LogInformation("User {UserId} accepted LocaGuest invitation into org {OrgId}", user.Id, consumed.OrganizationId);

            return Result.Success(new AcceptLocaGuestInvitationResponse
            {
                UserId = user.Id,
                Email = user.Email ?? email,
                OrganizationId = consumed.OrganizationId,
                Role = AuthGate.Auth.Domain.Constants.Roles.Occupant,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting LocaGuest invitation");
            return Result.Failure<AcceptLocaGuestInvitationResponse>($"Failed to accept invitation: {ex.Message}");
        }
    }
}
