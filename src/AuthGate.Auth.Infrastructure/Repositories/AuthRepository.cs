using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthGate.Auth.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly AuthGateDbContext _db;
    public AuthRepository(AuthGateDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email)
        => _db.Users.Include(u => u.LoginAttempts).FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByIdAsync(Guid id)
    => _db.Users.Include(u => u.DeviceSessions).FirstOrDefaultAsync(u => u.Id == id);

    public Task<IEnumerable<User>> GetAllAsync() =>
       Task.FromResult<IEnumerable<User>>(_db.Users.AsNoTracking().AsEnumerable());

    public async Task AddUserAsync(User user)
    {
        _db.Users.Add(user);
        //await _db.SaveChangesAsync();
    }

    public async Task AddDeviceSessionAsync(DeviceSession session)
    {
        _db.DeviceSessions.Add(session);
        //await _db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _db.Users.Include(u => u.DeviceSessions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return;

        // Marquer comme supprimé
        user.IsDeleted = true;

        // Révoquer ses sessions
        foreach (var s in user.DeviceSessions)
            s.RevokedAtUtc = DateTime.UtcNow;

        //await _db.SaveChangesAsync();
    }

    public Task<DeviceSession?> GetSessionByRefreshTokenAsync(string refreshToken) =>
    _db.DeviceSessions.Include(s => s) // pas de navs ici, mais prêt si tu ajoutes User
       .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

    public async Task RevokeSessionAsync(DeviceSession s, string? replacedByToken = null)
    {
        s.RevokedAtUtc = DateTime.UtcNow;
        s.ReplacedByToken = replacedByToken;
       // await _db.SaveChangesAsync();
    }

    public async Task RevokeAllOtherSessionsAsync(Guid userId, Guid exceptSessionId)
    {
        var sessions = await _db.DeviceSessions
            .Where(x => x.UserId == userId && x.Id != exceptSessionId && x.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var sess in sessions)
            sess.RevokedAtUtc = DateTime.UtcNow;

        //await _db.SaveChangesAsync();
    }

    public Task<IEnumerable<DeviceSession>> ListSessionsAsync(Guid userId) =>
        Task.FromResult<IEnumerable<DeviceSession>>(
            _db.DeviceSessions.Where(x => x.UserId == userId).AsEnumerable()
        );
    public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
    {
        _db.PasswordResetTokens.Add(token);
        //await _db.SaveChangesAsync();
    }

    public Task<PasswordResetToken?> GetValidResetTokenAsync(string token) =>
        _db.PasswordResetTokens
           .FirstOrDefaultAsync(t => t.Token == token && t.UsedAtUtc == null && DateTime.UtcNow < t.ExpiresAtUtc);

    public async Task MarkResetTokenUsedAsync(PasswordResetToken token)
    {
        token.UsedAtUtc = DateTime.UtcNow;
       // await _db.SaveChangesAsync();
    }
    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
