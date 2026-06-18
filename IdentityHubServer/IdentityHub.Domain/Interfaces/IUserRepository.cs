using IdentityHub.Domain.Entities;

namespace IdentityHub.Domain.Interfaces;

public interface IUserRepository
{
    Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastLoginAtAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetActiveSessionsCountAsync(string userId, CancellationToken cancellationToken = default);
    Task CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task DeleteAsync(ApplicationUser user, string? deletedBy, CancellationToken cancellationToken = default);
    Task UpdateRolesAsync(ApplicationUser user, IList<string> roles, CancellationToken cancellationToken = default);
}