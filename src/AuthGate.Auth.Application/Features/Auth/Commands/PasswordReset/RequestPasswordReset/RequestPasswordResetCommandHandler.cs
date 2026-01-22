using AuthGate.Auth.Application.Common;
using AuthGate.Auth.Application.Common.Interfaces;
using AuthGate.Auth.Domain.Entities;
using LocaGuest.Emailing.Abstractions;
using MediatR;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Application.Features.Auth.Commands.PasswordReset.RequestPasswordReset;

/// <summary>
/// Handler for RequestPasswordResetCommand
/// </summary>
public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailingService _emailing;
    private readonly IConfiguration _configuration;
    private readonly DbContext _context;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        UserManager<User> userManager,
        IEmailingService emailing,
        IConfiguration configuration,
        DbContext context,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _userManager = userManager;
        _emailing = emailing;
        _configuration = configuration;
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);

        // For security: Always return success even if user not found
        // This prevents email enumeration attacks
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return Result.Success(true);
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Password reset requested for inactive user: {Email}", request.Email);
            return Result.Success(true);
        }

        // Generate password reset token using Identity
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Store token in database for tracking
        var passwordResetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
            IsUsed = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Set<PasswordResetToken>().AddAsync(passwordResetToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Send reset email
        var frontendUrl = _configuration["Frontend:ResetPasswordUrl"] ?? "http://localhost:4200/reset-password";
        var resetUrl = QueryHelpers.AddQueryString(
            uri: frontendUrl,
            queryString: new Dictionary<string, string?>
            {
                ["token"] = resetToken,
                ["email"] = request.Email
            });

        var emailBody = $@"
            <h2>Password Reset Request</h2>
            <p>Hello,</p>
            <p>We received a request to reset your password. Click the link below to reset your password:</p>
            <p><a href=""{resetUrl}"">Reset Password</a></p>
            <p>This link will expire in 1 hour.</p>
            <p>If you did not request a password reset, please ignore this email.</p>
            <br/>
            <p>Best regards,<br/>AuthGate Team</p>
        ";

        await _emailing.QueueHtmlAsync(
            toEmail: request.Email,
            subject: "Password Reset Request",
            htmlContent: emailBody,
            textContent: null,
            attachments: null,
            tags: EmailUseCaseTags.AuthResetPassword,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Password reset email sent to: {Email}", request.Email);

        return Result.Success(true);
    }
}
