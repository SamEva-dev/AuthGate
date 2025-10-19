using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Interfaces.Repositories;

public interface IPermissionRepository
{
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<Permission?> GetByCodeAsync(string code);
    Task AddAsync(Permission permission);
    Task AssignToRoleAsync(Guid roleId, Guid permissionId);
    Task RemoveFromRoleAsync(Guid roleId, Guid permissionId);
}