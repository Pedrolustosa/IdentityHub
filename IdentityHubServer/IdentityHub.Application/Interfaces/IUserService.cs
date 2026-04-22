using IdentityHub.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.Interfaces
{
    public interface IUserService
    {
        Task<List<UserResponse>> GetAllAsync();
        Task<UserResponse?> GetByIdAsync(string id);
        Task CreateAsync(CreateUserRequest request);
        Task UpdateAsync(string id, UpdateUserRequest request);
        Task DeleteAsync(string id);
        Task UpdateRolesAsync(string id, UpdateRolesRequest request);
    }
}
