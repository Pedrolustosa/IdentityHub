using IdentityHub.Domain.Entities;

namespace IdentityHub.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default);
        Task CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default);
        Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);
        Task ReplaceUserRolesAsync(ApplicationUser user, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default);
    }
}
