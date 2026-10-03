using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

public interface IPermissionVersionService
{
    Task BumpUserAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task BumpUsersInRoleAsync(string? roleName, CancellationToken cancellationToken = default);
}
