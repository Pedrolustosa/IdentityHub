using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<RoleResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
        Task UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        Task<List<string>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken = default);
        Task UpdatePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default);
    }
}
