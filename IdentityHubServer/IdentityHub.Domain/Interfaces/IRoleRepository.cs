using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityHub.Domain.Interfaces;

public interface IRoleRepository
{
    Task<List<IdentityRole>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IdentityRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IdentityRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task CreateAsync(IdentityRole role, CancellationToken cancellationToken = default);
    Task UpdateAsync(IdentityRole role, CancellationToken cancellationToken = default);
    Task DeleteAsync(IdentityRole role, CancellationToken cancellationToken = default);

    Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default);
    Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default);
    Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default);
}