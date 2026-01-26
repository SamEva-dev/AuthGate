using AuthGate.Auth.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthGate.Auth.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AuthDbContext _context;
    private readonly AuditDbContext _auditContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(
        AuthDbContext context,
        AuditDbContext auditContext,
        IUserRepository users,
        IRoleRepository roles,
        IPermissionRepository permissions,
        IRefreshTokenRepository refreshTokens,
        IUserOrganizationRepository userOrganizations,
        IUserAppAccessRepository userAppAccess,
        IAuditLogRepository auditLogs)
    {
        _context = context;
        _auditContext = auditContext;
        Users = users;
        Roles = roles;
        Permissions = permissions;
        RefreshTokens = refreshTokens;
        UserOrganizations = userOrganizations;
        UserAppAccess = userAppAccess;
        AuditLogs = auditLogs;
    }

    public IUserRepository Users { get; }
    public IRoleRepository Roles { get; }
    public IPermissionRepository Permissions { get; }
    public IRefreshTokenRepository RefreshTokens { get; }
    public IUserOrganizationRepository UserOrganizations { get; }
    public IUserAppAccessRepository UserAppAccess { get; }
    public IAuditLogRepository AuditLogs { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var authChanges = await _context.SaveChangesAsync(cancellationToken);
        await _auditContext.SaveChangesAsync(cancellationToken);
        return authChanges;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _auditContext.SaveChangesAsync(cancellationToken);
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        _auditContext.Dispose();
    }
}
