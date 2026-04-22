using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace IdentityHub.Infrastructure.Services
{
    public class RoleRepository : IRoleRepository
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleRepository(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public Task<List<IdentityRole>> GetAllAsync()
            => Task.FromResult(_roleManager.Roles.ToList());

        public async Task<IdentityRole?> GetByIdAsync(string id)
            => await _roleManager.FindByIdAsync(id);

        public async Task<IdentityRole?> GetByNameAsync(string name)
            => await _roleManager.FindByNameAsync(name);

        public async Task CreateAsync(IdentityRole role)
        {
            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
                throw new Exception("Error creating role");
        }

        public async Task UpdateAsync(IdentityRole role)
        {
            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
                throw new Exception("Error updating role");
        }

        public async Task DeleteAsync(IdentityRole role)
        {
            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
                throw new Exception("Error deleting role");
        }

        public async Task<IList<Claim>> GetClaimsAsync(IdentityRole role)
            => await _roleManager.GetClaimsAsync(role);

        public async Task AddClaimAsync(IdentityRole role, Claim claim)
            => await _roleManager.AddClaimAsync(role, claim);

        public async Task RemoveClaimAsync(IdentityRole role, Claim claim)
            => await _roleManager.RemoveClaimAsync(role, claim);
    }
}
