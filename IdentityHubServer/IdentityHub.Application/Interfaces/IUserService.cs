using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces
{
    public interface IUserService
    {
        Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<UserResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task UpdateAsync(string id, UpdateUserRequest request, string? actingUserId, CancellationToken cancellationToken = default);
        Task UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken cancellationToken = default);
    }
}
