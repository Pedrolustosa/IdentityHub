using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;

namespace IdentityHub.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var users = await _repository.GetAllAsync(cancellationToken);
            var list = new List<UserResponse>();

            foreach (var u in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var roles = await _repository.GetRolesAsync(u, cancellationToken);
                list.Add(new UserResponse
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    IsActive = u.IsActive,
                    Roles = roles.ToList()
                });
            }

            return list;
        }

        public async Task<UserResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _repository.GetByIdAsync(id, cancellationToken);
            if (user == null)
                return null;

            var roles = await _repository.GetRolesAsync(user, cancellationToken);
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };
        }

        public async Task CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email is required");

            var normalizedEmail = request.Email.Trim().ToLower();
            var user = new ApplicationUser
            {
                Email = normalizedEmail,
                UserName = normalizedEmail,
                FullName = request.FullName,
                IsActive = true
            };

            await _repository.CreateAsync(user, request.Password, cancellationToken);
        }

        public async Task UpdateAsync(string id, UpdateUserRequest request, string? actingUserId, CancellationToken cancellationToken = default)
        {
            var user = await _repository.GetByIdAsync(id, cancellationToken);
            if (user == null)
                throw new Exception("User not found");

            user.FullName = request.FullName;

            var isSelf =
                !string.IsNullOrEmpty(actingUserId) &&
                string.Equals(id, actingUserId, StringComparison.Ordinal);

            if (isSelf && user.IsActive != request.IsActive)
                throw new Exception("You cannot activate or deactivate your own account.");

            if (!isSelf)
                user.IsActive = request.IsActive;

            await _repository.UpdateAsync(user, cancellationToken);
        }

        public async Task UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _repository.GetByIdAsync(id, cancellationToken);
            if (user == null)
                throw new Exception("User not found");

            await _repository.ReplaceUserRolesAsync(user, request.Roles ?? new List<string>(), cancellationToken);
        }
    }
}
