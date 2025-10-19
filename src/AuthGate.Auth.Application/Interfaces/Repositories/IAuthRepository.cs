using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Interfaces.Repositories;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllAsync();

    Task AddUserAsync(User user);
    Task AddDeviceSessionAsync(DeviceSession session);

    Task<DeviceSession?> GetSessionByRefreshTokenAsync(string refreshToken);
    Task RevokeSessionAsync(DeviceSession s, string? replacedByToken = null);
    Task RevokeAllOtherSessionsAsync(Guid userId, Guid exceptSessionId);
    Task<IEnumerable<DeviceSession>> ListSessionsAsync(Guid userId);

    Task AddPasswordResetTokenAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetValidResetTokenAsync(string token);
    Task MarkResetTokenUsedAsync(PasswordResetToken token);

    Task DeleteUserAsync(Guid userId);

    Task SaveChangesAsync();
}