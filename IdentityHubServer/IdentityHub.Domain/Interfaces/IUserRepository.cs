using IdentityHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string id);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<List<ApplicationUser>> GetAllAsync();
        Task CreateAsync(ApplicationUser user, string password);
        Task UpdateAsync(ApplicationUser user);
        Task DeleteAsync(ApplicationUser user);

        Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user);
        Task ReplaceUserRolesAsync(ApplicationUser user, IReadOnlyList<string> roleNames);
    }
}
