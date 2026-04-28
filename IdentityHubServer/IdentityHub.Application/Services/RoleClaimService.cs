using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;
using System.Security.Claims;

namespace IdentityHub.Application.Services
{
    public class RoleClaimService : IRoleClaimService
    {
        private readonly IRoleRepository _repository;

        public RoleClaimService(IRoleRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<string>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
                throw new Exception("Role not found");

            var claims = await _repository.GetClaimsAsync(role, cancellationToken);
            return claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
        }

        public async Task AddPermissionAsync(string roleId, string permission, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
                throw new Exception("Role not found");

            var claims = await _repository.GetClaimsAsync(role, cancellationToken);
            var exists = claims.Any(c => c.Type == "permission" && c.Value == permission);
            if (exists)
                throw new Exception("Permission already exists");

            await _repository.AddClaimAsync(role, new Claim("permission", permission), cancellationToken);
        }

        public async Task RemovePermissionAsync(string roleId, string permission, CancellationToken cancellationToken = default)
        {
            var role = await _repository.GetByIdAsync(roleId, cancellationToken);
            if (role == null)
                throw new Exception("Role not found");

            var claims = await _repository.GetClaimsAsync(role, cancellationToken);
            var claim = claims.FirstOrDefault(c => c.Type == "permission" && c.Value == permission);
            if (claim == null)
                return;

            await _repository.RemoveClaimAsync(role, claim, cancellationToken);
        }

        public async Task ReplacePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default)
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

            foreach (var permission in permissions.Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _repository.AddClaimAsync(role, new Claim("permission", permission), cancellationToken);
            }
        }
    }
}
