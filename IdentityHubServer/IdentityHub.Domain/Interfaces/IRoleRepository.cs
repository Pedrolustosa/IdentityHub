using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace IdentityHub.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<IdentityRole>> GetAllAsync();
        Task<IdentityRole?> GetByIdAsync(string id);
        Task<IdentityRole?> GetByNameAsync(string name);
        Task CreateAsync(IdentityRole role);
        Task UpdateAsync(IdentityRole role);
        Task DeleteAsync(IdentityRole role);

        Task<IList<Claim>> GetClaimsAsync(IdentityRole role);
        Task AddClaimAsync(IdentityRole role, Claim claim);
        Task RemoveClaimAsync(IdentityRole role, Claim claim);
    }
}
