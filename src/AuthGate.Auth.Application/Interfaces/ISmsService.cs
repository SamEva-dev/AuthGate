
namespace AuthGate.Auth.Application.Interfaces;

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}