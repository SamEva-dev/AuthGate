using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Domain.Repositories;
using AuthGate.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthGate.Auth.Infrastructure.Repositories;

public class TrustedDeviceRepository : ITrustedDeviceRepository
{
    private readonly AuthDbContext _context;

    public TrustedDeviceRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<TrustedDevice?> GetByUserAndFingerprintAsync(Guid userId, string deviceFingerprint, CancellationToken cancellationToken = default)
    {
        return await _context.TrustedDevices
            .FirstOrDefaultAsync(
                td => td.UserId == userId 
                && td.DeviceFingerprint == deviceFingerprint 
                && !td.IsRevoked 
                && td.ExpiresAtUtc > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<IEnumerable<TrustedDevice>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.TrustedDevices
            .Where(td => td.UserId == userId && !td.IsRevoked)
            .OrderByDescending(td => td.LastUsedAtUtc ?? td.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TrustedDevice trustedDevice, CancellationToken cancellationToken = default)
    {
        await _context.TrustedDevices.AddAsync(trustedDevice, cancellationToken);
    }

    public void Update(TrustedDevice trustedDevice)
    {
        _context.TrustedDevices.Update(trustedDevice);
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var device = await _context.TrustedDevices.FindAsync(new object[] { id }, cancellationToken);
        if (device == null) return false;

        device.Revoke();
        _context.TrustedDevices.Update(device);
        return true;
    }

    public async Task RevokeAllUserDevicesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var devices = await _context.TrustedDevices
            .Where(td => td.UserId == userId && !td.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var device in devices)
        {
            device.Revoke();
        }

        _context.TrustedDevices.UpdateRange(devices);
    }

    public async Task<int> DeleteExpiredDevicesAsync(CancellationToken cancellationToken = default)
    {
        var expiredDevices = await _context.TrustedDevices
            .Where(td => td.ExpiresAtUtc < DateTime.UtcNow || td.IsRevoked)
            .ToListAsync(cancellationToken);

        _context.TrustedDevices.RemoveRange(expiredDevices);
        return expiredDevices.Count;
    }
}
