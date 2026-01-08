using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _configuration;

    public RefreshTokenRepository(AuthDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshTokenOrNull(token);
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token || (tokenHash != null && rt.Token == tokenHash), cancellationToken);
    }

    private string? HashRefreshTokenOrNull(string refreshToken)
    {
        var pepper = _configuration["Jwt:RefreshTokenPepper"];
        if (string.IsNullOrWhiteSpace(pepper))
        {
            pepper = _configuration["Security:RefreshTokenPepper"];
        }

        if (string.IsNullOrWhiteSpace(pepper))
        {
            return null;
        }

        var input = Encoding.UTF8.GetBytes(refreshToken + pepper);
        var hash = SHA256.HashData(input);
        return Convert.ToHexString(hash);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevocationReason = reason;
        }
    }

    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAtUtc < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.RefreshTokens.RemoveRange(expiredTokens);
        return expiredTokens.Count;
    }
}
