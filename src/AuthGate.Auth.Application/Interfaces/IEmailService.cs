
namespace AuthGate.Auth.Application.Interfaces;

public interface IEmailService
{
    Task SendValidationEmailAsync(string to, string token);
    Task SendResetPasswordAsync(string to, string token);
}
