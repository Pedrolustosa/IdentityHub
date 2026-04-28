using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Services
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRepository(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _userManager.Users.ToListAsync(cancellationToken);
        }

        public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _userManager.FindByIdAsync(id);
        }

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _userManager.FindByEmailAsync(email);
        }

        public async Task CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception("Error creating user");
        }

        public async Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _userManager.UpdateAsync(user);
        }

        public async Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task ReplaceUserRolesAsync(ApplicationUser user, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default)
        {
            var normalized = roleNames
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            foreach (var name in normalized)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await _roleManager.RoleExistsAsync(name))
                    throw new Exception($"Role not found: {name}");
            }

            var current = (await _userManager.GetRolesAsync(user)).ToArray();
            if (current.Length > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var removeResult = await _userManager.RemoveFromRolesAsync(user, current);
                if (!removeResult.Succeeded)
                    throw new Exception(string.Join(",", removeResult.Errors.Select(e => e.Description)));
            }

            if (normalized.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var addResult = await _userManager.AddToRolesAsync(user, normalized);
                if (!addResult.Succeeded)
                    throw new Exception(string.Join(",", addResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
