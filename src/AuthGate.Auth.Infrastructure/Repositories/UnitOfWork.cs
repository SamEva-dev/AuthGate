using AuthGate.Auth.Application.Interfaces.Repositories;
using AuthGate.Auth.Domain.Entities;
using AuthGate.Auth.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthGate.Auth.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AuthGateDbContext _context;

    public UnitOfWork(AuthGateDbContext context)
    {
        _context = context;
        Auth = new AuthRepository(_context);
        Roles = new RoleRepository(_context);
        Permissions = new PermissionRepository(_context);
    }

    public IAuthRepository Auth { get; }
    public IRoleRepository Roles { get; }
    public IPermissionRepository Permissions { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            //_logger.LogWarning("⚠️ Concurrency exception: {Msg}", ex.Message);
            //foreach (var entry in ex.Entries)
            //  _logger.LogWarning("Entity: {Entity}, State: {State}", entry.Entity.GetType().Name, entry.State);

            // On peut relancer une lecture pour resynchroniser
            foreach (var entry in ex.Entries)
            {
                if (entry.Entity is DeviceSession)
                {
                    entry.Reload();
                }
            }

            // Retry une fois
            return await _context.SaveChangesAsync(ct);
        }
    }

    public void Dispose() => _context.Dispose();
}