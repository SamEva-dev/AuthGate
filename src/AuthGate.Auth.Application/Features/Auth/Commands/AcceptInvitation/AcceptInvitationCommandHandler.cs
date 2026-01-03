using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.AcceptInvitation;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Result<AcceptInvitationResponse>>
{
    private readonly IAuthDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        IAuthDbContext context,
        UserManager<User> userManager,
        ITokenService tokenService,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _context = context;
        _userManager = userManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<AcceptInvitationResponse>> Handle(
        AcceptInvitationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _context.UserInvitations
                .FirstOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

            if (invitation == null || !invitation.IsValid())
            {
                return Result.Failure<AcceptInvitationResponse>("Invalid or expired invitation");
            }

            var existingUser = await _userManager.FindByEmailAsync(invitation.Email);
            if (existingUser != null)
            {
                return Result.Failure<AcceptInvitationResponse>("User already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = invitation.Email,
                Email = invitation.Email,
                EmailConfirmed = true,
                FirstName = request.FirstName,
                LastName = request.LastName,
                OrganizationId = invitation.OrganizationId,
                IsActive = true,
                MfaEnabled = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Result.Failure<AcceptInvitationResponse>($"Failed to create user: {errors}");
            }

            await _userManager.AddToRoleAsync(user, invitation.Role);

            invitation.Accept(user.Id);
            await _context.SaveChangesAsync(cancellationToken);

            var (accessToken, refreshToken) = await _tokenService.GenerateTokensAsync(user);

            _logger.LogInformation("User {UserId} accepted invitation {InvitationId}", user.Id, invitation.Id);

            return Result<AcceptInvitationResponse>.Success(new AcceptInvitationResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                OrganizationId = invitation.OrganizationId,
                OrganizationCode = invitation.OrganizationCode,
                OrganizationName = invitation.OrganizationName,
                Role = invitation.Role,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation");
            return Result.Failure<AcceptInvitationResponse>($"Failed to accept invitation: {ex.Message}");
        }
    }
}
