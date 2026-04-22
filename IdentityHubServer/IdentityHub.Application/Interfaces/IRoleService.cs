using IdentityHub.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleResponse>> GetAllAsync();
        Task<RoleResponse?> GetByIdAsync(string id);
        Task CreateAsync(CreateRoleRequest request);
        Task UpdateAsync(string id, UpdateRoleRequest request);
        Task DeleteAsync(string id);

        Task<List<string>> GetPermissionsAsync(string roleId);
        Task UpdatePermissionsAsync(string roleId, List<string> permissions);
    }
}
