using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IRoleService
{
    Task<Result<List<RoleResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<RoleResponse>> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Result> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken);
    Task<Result<List<string>>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken);
    Task<Result> UpdatePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken);
}