using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Application.Services;
using AuthGate.Auth.Domain.Constants;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Enums;
using LocaGuest.Emailing.Abstractions;
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
    private readonly IEmailingService _emailing;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InviteCollaboratorCommandHandler> _logger;

    public InviteCollaboratorCommandHandler(
        IAuthDbContext context,
        UserManager<User> userManager,
        ICurrentUserService currentUserService,
        IOrganizationContext organizationContext,
        IEmailingService emailing,
        IConfiguration configuration,
        ILogger<InviteCollaboratorCommandHandler> logger)
    {
        _context = context;
        _userManager = userManager;
        _currentUserService = currentUserService;
        _organizationContext = organizationContext;
        _emailing = emailing;
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
            if (!inviterRoles.Contains(Domain.Constants.Roles.TenantOwner) &&
                !inviterRoles.Contains(Domain.Constants.Roles.TenantAdmin) &&
                !inviterRoles.Contains(Domain.Constants.Roles.SuperAdmin))
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
            var allowedRoles = new[] { Domain.Constants.Roles.TenantAdmin,
                Domain.Constants.Roles.TenantManager,
                Domain.Constants.Roles.SuperAdmin,
                Domain.Constants.Roles.TenantUser, 
                Domain.Constants.Roles.ReadOnly };
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

            // 6. Create invitation using factory method (generates secure token + hash)
            var (invitation, rawToken) = UserInvitation.Create(
                organizationId: organizationId,
                organizationCode: organizationCode,
                organizationName: organizationName,
                email: request.Email,
                role: request.Role,
                invitedBy: inviterId.Value,
                message: request.Message,
                expirationDays: 7
            );

            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync(cancellationToken);

            // 7. Generate invitation URL (rawToken contains {id}.{secret})
            var frontendUrl = _configuration["Frontend:BaseUrl"];
            var invitationUrl = $"{frontendUrl}/accept-invitation/{rawToken}";

            // 8. ✅ Send invitation email
            try
            {
                var toName = request.Email.Split('@')[0];
                var inviterName = $"{inviter.FirstName} {inviter.LastName}";
                var subject = $"Invitation à rejoindre {organizationName} sur LocaGuest";
                var htmlBody = $$"""
<h2>Invitation à rejoindre une équipe</h2>
<p>Bonjour {{toName}},</p>
<p><strong>{{inviterName}}</strong> vous invite à rejoindre <strong>{{organizationName}}</strong> sur LocaGuest.</p>
<p><strong>Votre rôle:</strong> {{request.Role}}</p>
<p><a href="{{invitationUrl}}">Accepter l'invitation</a></p>
<p style="font-size: 14px; color: #6b7280;">Cette invitation expire le <strong>{{invitation.ExpiresAt:dd/MM/yyyy à HH:mm}}</strong></p>
<p>Si vous n'avez pas demandé cette invitation, vous pouvez ignorer cet email en toute sécurité.</p>
""";

                await _emailing.QueueHtmlAsync(
                    toEmail: request.Email,
                    subject: subject,
                    htmlContent: htmlBody,
                    textContent: null,
                    attachments: null,
                    tags: EmailUseCaseTags.AccessInviteUser,
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
