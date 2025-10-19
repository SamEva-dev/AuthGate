
using AuthGate.Auth.Domain.Entities;

namespace AuthGate.Auth.Application.Interfaces.Repositories;

public interface IRoleRepository
{
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByNameAsync(string name);
    Task AddAsync(Role role);
    Task DeleteAsync(Guid id);
    Task AssignRoleAsync(Guid userId, Guid roleId);
    Task RemoveRoleAsync(Guid userId, Guid roleId);
}