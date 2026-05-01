using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Interfaces;

public interface IUserService
{
    Task<Result<List<UserResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result<UserResponse>> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Result> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(string id, CancellationToken cancellationToken);
    Task<Result> UpdateRolesAsync(string id, UpdateRolesRequest request, CancellationToken cancellationToken);
}