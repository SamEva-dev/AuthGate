using AuthGate.Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthGate.Auth.Infrastructure.Identity;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    public EmailService(ILogger<EmailService> logger) => _logger = logger;

    public Task SendValidationEmailAsync(string to, string token)
    {
        _logger.LogInformation("SendValidationEmail -> to:{To}, token:{Token}", to, token);
        return Task.CompletedTask;
    }

    public Task SendResetPasswordAsync(string to, string token)
    {
        _logger.LogInformation("SendResetPassword -> to:{To}, token:{Token}", to, token);
        return Task.CompletedTask;
    }
}