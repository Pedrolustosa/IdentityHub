using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace IdentityHub.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repository;

        public RoleService(IRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoleResponse>> GetAllAsync()
        {
            var roles = await _repository.GetAllAsync();

            return roles.Select(r => new RoleResponse
            {
                Id = r.Id,
                Name = r.Name
            }).ToList();
        }

        public async Task<RoleResponse?> GetByIdAsync(string id)
        {
            var role = await _repository.GetByIdAsync(id);

            if (role == null) return null;

            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name
            };
        }

        public async Task CreateAsync(CreateRoleRequest request)
        {
            var exists = await _repository.GetByNameAsync(request.Name);

            if (exists != null)
                throw new Exception("Role already exists");

            await _repository.CreateAsync(new IdentityRole(request.Name));
        }

        public async Task UpdateAsync(string id, UpdateRoleRequest request)
        {
            var role = await _repository.GetByIdAsync(id);

            if (role == null)
                throw new Exception("Role not found");

            role.Name = request.Name;

            await _repository.UpdateAsync(role);
        }

        public async Task DeleteAsync(string id)
        {
            var role = await _repository.GetByIdAsync(id);

            if (role == null)
                throw new Exception("Role not found");

            await _repository.DeleteAsync(role);
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

        public async Task UpdatePermissionsAsync(string roleId, List<string> permissions)
        {
            var role = await _repository.GetByIdAsync(roleId);

            if (role == null)
                throw new Exception("Role not found");

            var currentClaims = await _repository.GetClaimsAsync(role);

            var currentPermissions = currentClaims
                .Where(c => c.Type == "permission")
                .ToList();

            // remove atuais
            foreach (var claim in currentPermissions)
                await _repository.RemoveClaimAsync(role, claim);

            // adiciona novas
            foreach (var permission in permissions)
            {
                await _repository.AddClaimAsync(
                    role,
                    new Claim("permission", permission));
            }
        }
    }
}
