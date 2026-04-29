using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdentityHub.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleRepository(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public Task<List<IdentityRole>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _roleManager.Roles.ToListAsync(cancellationToken);
        }

        public Task<IdentityRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _roleManager.FindByIdAsync(id);
        }

        public Task<IdentityRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _roleManager.FindByNameAsync(name);
        }

        public async Task CreateAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new Exception("Error creating role");
        }

        public async Task UpdateAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
                throw new Exception("Error updating role");
        }

        public async Task DeleteAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
                throw new Exception("Error deleting role");
        }

        public async Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _roleManager.GetClaimsAsync(role);
        }

        public async Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _roleManager.AddClaimAsync(role, claim);
        }

        public async Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _roleManager.RemoveClaimAsync(role, claim);
        }
    }
}
