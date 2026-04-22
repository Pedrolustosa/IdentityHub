using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace IdentityHub.Application.Services
{
    public class RoleClaimService : IRoleClaimService
    {
        private readonly IRoleRepository _repository;

        public RoleClaimService(IRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<string>> GetPermissionsAsync(string roleId)
        {
            var role = await _repository.GetByIdAsync(roleId);

            if (role == null)
                throw new Exception("Role not found");

            var claims = await _repository.GetClaimsAsync(role);

            return claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();
        }

        public async Task AddPermissionAsync(string roleId, string permission)
        {
            var role = await _repository.GetByIdAsync(roleId);

            if (role == null)
                throw new Exception("Role not found");

            var claims = await _repository.GetClaimsAsync(role);

            var exists = claims.Any(c =>
                c.Type == "permission" && c.Value == permission);

            if (exists)
                throw new Exception("Permission already exists");

            await _repository.AddClaimAsync(role, new Claim("permission", permission));
        }

        public async Task RemovePermissionAsync(string roleId, string permission)
        {
            var role = await _repository.GetByIdAsync(roleId);

            if (role == null)
                throw new Exception("Role not found");

            var claims = await _repository.GetClaimsAsync(role);

            var claim = claims.FirstOrDefault(c =>
                c.Type == "permission" && c.Value == permission);

            if (claim == null)
                return;

            await _repository.RemoveClaimAsync(role, claim);
        }

        public async Task ReplacePermissionsAsync(string roleId, List<string> permissions)
        {
            var role = await _repository.GetByIdAsync(roleId);

            if (role == null)
                throw new Exception("Role not found");

            var currentClaims = await _repository.GetClaimsAsync(role);

            var currentPermissions = currentClaims
                .Where(c => c.Type == "permission")
                .ToList();

            foreach (var claim in currentPermissions)
                await _repository.RemoveClaimAsync(role, claim);

            foreach (var permission in permissions.Distinct())
            {
                await _repository.AddClaimAsync(
                    role,
                    new Claim("permission", permission));
            }
        }
    }
}
