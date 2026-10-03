using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.Services;

public sealed class PermissionVersionService : IPermissionVersionService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionVersionService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task BumpUserAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        user.PermissionVersion++;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    public async Task BumpUsersInRoleAsync(string? roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        foreach (var user in usersInRole)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await BumpUserAsync(user, cancellationToken);
        }
    }
}
