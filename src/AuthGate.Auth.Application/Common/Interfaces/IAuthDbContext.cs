using AuthGate.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AuthGate.Auth.Application.Common.Interfaces;

/// <summary>
/// Interface for AuthGate database context
/// </summary>
public interface IAuthDbContext
{
    DbSet<UserInvitation> UserInvitations { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
