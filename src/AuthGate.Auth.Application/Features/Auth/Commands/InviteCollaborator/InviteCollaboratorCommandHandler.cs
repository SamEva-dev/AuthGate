using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Application.Services.Email;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.InviteCollaborator;

public class InviteCollaboratorCommandHandler : IRequestHandler<InviteCollaboratorCommand, Result<InviteCollaboratorResponse>>
{
    private readonly IAuthDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOrganizationContext _organizationContext;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InviteCollaboratorCommandHandler> _logger;

    public InviteCollaboratorCommandHandler(
        IAuthDbContext context,
        UserManager<User> userManager,
        ICurrentUserService currentUserService,
        IOrganizationContext organizationContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<InviteCollaboratorCommandHandler> logger)
    {
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
        _organizationContext = organizationContext;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<InviteCollaboratorResponse>> Handle(
        InviteCollaboratorCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate inviter has permission (TenantOwner or TenantAdmin)
            var inviterId = _currentUserService.UserId;
            if (!inviterId.HasValue)
            {
                return Result.Failure<InviteCollaboratorResponse>("User not authenticated");
            }

            var inviter = await _userManager.FindByIdAsync(inviterId.Value.ToString());
            if (inviter == null)
            {
                return Result.Failure<InviteCollaboratorResponse>("Inviter not found");
            }

            var inviterRoles = await _userManager.GetRolesAsync(inviter);
            if (!inviterRoles.Contains(Domain.Constants.Roles.TenantOwner) && !inviterRoles.Contains(Domain.Constants.Roles.TenantAdmin))
            {
                return Result.Failure<InviteCollaboratorResponse>("Only TenantOwner or TenantAdmin can invite users");
            }

            // 2. Validate tenant context
            if (!_organizationContext.OrganizationId.HasValue)
            {
                return Result.Failure<InviteCollaboratorResponse>("Tenant context not found");
            }

            var organizationId = _organizationContext.OrganizationId.Value;
            var organizationCode = _organizationContext.OrganizationCode ?? "Unknown";
            var organizationName = _organizationContext.OrganizationName ?? "Unknown Organization";

            // 3. Validate role
            var allowedRoles = new[] { Domain.Constants.Roles.TenantAdmin, Domain.Constants.Roles.TenantManager, Domain.Constants.Roles.TenantUser, Domain.Constants.Roles.ReadOnly };
            if (!allowedRoles.Contains(request.Role))
            {
                return Result.Failure<InviteCollaboratorResponse>(
                    $"Invalid role. Allowed roles: {string.Join(", ", allowedRoles)}");
            }

            // 4. Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result.Failure<InviteCollaboratorResponse>($"User with email '{request.Email}' already exists");
            }

            // 5. Check if invitation already exists and is valid
            var existingInvitation = await _context.UserInvitations
                .Where(i => i.Email == request.Email && i.OrganizationId == organizationId && i.Status == InvitationStatus.Pending)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingInvitation != null && existingInvitation.IsValid())
            {
                return Result.Failure<InviteCollaboratorResponse>("An active invitation already exists for this email");
            }

            // 6. Create invitation
            var token = Guid.NewGuid().ToString("N"); // Secure random token
            var expiresAt = DateTime.UtcNow.AddDays(7); // 7 days validity

            var invitation = new UserInvitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                OrganizationCode = organizationCode,
                OrganizationName = organizationName,
                Email = request.Email,
                Role = request.Role,
                Token = token,
                InvitedBy = inviterId.Value,
                Status = InvitationStatus.Pending,
                ExpiresAt = expiresAt,
                Message = request.Message,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = inviterId.Value
            };

            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync(cancellationToken);

            // 7. Generate invitation URL
            var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
            var invitationUrl = $"{frontendUrl}/accept-invitation/{token}";

            // 8. ✅ Send invitation email
            try
            {
                await _emailService.SendInvitationEmailAsync(
                    toEmail: request.Email,
                    toName: request.Email.Split('@')[0], // Use email prefix as default name
                    inviterName: $"{inviter.FirstName} {inviter.LastName}",
                    organizationName: organizationName,
                    role: request.Role,
                    invitationUrl: invitationUrl,
                    expiresAt: expiresAt,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Invitation email sent to {Email}", request.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, 
                    "Failed to send invitation email to {Email}, but invitation was created", 
                    request.Email);
                // Continue even if email fails - invitation is already created
            }

            _logger.LogInformation(
                "Invitation created: {Email} invited to {OrganizationCode} as {Role} by {InviterId}",
                request.Email, organizationCode, request.Role, inviterId.Value);

            var response = new InviteCollaboratorResponse
            {
                InvitationId = invitation.Id,
                Email = invitation.Email,
                Role = invitation.Role,
                InvitationUrl = invitationUrl,
                ExpiresAt = invitation.ExpiresAt
            };

            return Result<InviteCollaboratorResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invitation for {Email}", request.Email);
            return Result.Failure<InviteCollaboratorResponse>($"Failed to create invitation: {ex.Message}");
        }
    }
}
