using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityHub.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repository;

        public RoleService(IRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _repository.GetAllAsync(cancellationToken);
            return roles.Select(r => new RoleResponse
            {
                Id = r.Id,
                Name = r.Name
            }).ToList();
        }

        public async Task<RoleResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(id, cancellationToken);
            if (role == null)
                return null;

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name
            };
        }

        public async Task CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var exists = await _repository.GetByNameAsync(request.Name, cancellationToken);
            if (exists != null)
                throw new Exception("Role already exists");

            await _repository.CreateAsync(new IdentityRole(request.Name), cancellationToken);
        }

        public async Task UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(id, cancellationToken);
            if (role == null)
                throw new Exception("Role not found");

            role.Name = request.Name;
            await _repository.UpdateAsync(role, cancellationToken);
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(id, cancellationToken);
            if (role == null)
                throw new Exception("Role not found");

            await _repository.DeleteAsync(role, cancellationToken);
        }

        public async Task<List<string>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
                throw new Exception("Role not found");

            var claims = await _repository.GetClaimsAsync(role, cancellationToken);
            return claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();
        }

        public async Task UpdatePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
                throw new Exception("Role not found");

            var currentClaims = await _repository.GetClaimsAsync(role, cancellationToken);
            var currentPermissions = currentClaims.Where(c => c.Type == "permission").ToList();

            foreach (var claim in currentPermissions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _repository.RemoveClaimAsync(role, claim, cancellationToken);
            }

            foreach (var permission in permissions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _repository.AddClaimAsync(role, new Claim("permission", permission), cancellationToken);
            }
        }
    }
}
