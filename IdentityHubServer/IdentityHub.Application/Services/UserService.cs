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

        public async Task<List<UserResponse>> GetAllAsync()
        {
            var users = await _repository.GetAllAsync();
            var list = new List<UserResponse>();

            foreach (var u in users)
            {
                var roles = await _repository.GetRolesAsync(u);
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

        public async Task<UserResponse?> GetByIdAsync(string id)
        {
            var user = await _repository.GetByIdAsync(id);

            if (user == null) return null;

            var roles = await _repository.GetRolesAsync(user);

            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };
        }

        public async Task CreateAsync(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new Exception("Email is required");

            var user = new ApplicationUser
            {
                Email = request.Email.Trim().ToLower(),
                UserName = request.Email.Trim().ToLower(),
                FullName = request.FullName,
                IsActive = true
            };

            await _repository.CreateAsync(user, request.Password);
        }

        public async Task UpdateAsync(string id, UpdateUserRequest request)
        {
            var user = await _repository.GetByIdAsync(id);

            if (user == null)
                throw new Exception("User not found");

            user.FullName = request.FullName;
            user.IsActive = request.IsActive;

            await _repository.UpdateAsync(user);
        }

        public async Task DeleteAsync(string id)
        {
            var user = await _repository.GetByIdAsync(id);

            if (user == null)
                throw new Exception("User not found");

            await _repository.DeleteAsync(user);
        }

        public async Task UpdateRolesAsync(string id, UpdateRolesRequest request)
        {
            var user = await _repository.GetByIdAsync(id);

            if (user == null)
                throw new Exception("User not found");

            await _repository.ReplaceUserRolesAsync(user, request.Roles ?? new List<string>());
        }
    }
}
