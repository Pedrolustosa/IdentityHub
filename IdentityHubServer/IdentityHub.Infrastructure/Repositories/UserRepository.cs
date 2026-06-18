using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;

    public UserRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _userManager.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userManager.Users
            .FirstOrDefaultAsync(user => user.Id == id && !user.IsDeleted, cancellationToken);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedEmail = _userManager.NormalizeEmail(email);

        return await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userManager.GetRolesAsync(user);
    }

    public async Task<DateTime?> GetLastLoginAtAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _context.UserSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .Select(session => (DateTime?)(session.LastAccessAt ?? session.CreatedAt))
            .OrderByDescending(date => date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetActiveSessionsCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _context.UserSessions
            .AsNoTracking()
            .CountAsync(session => session.UserId == userId && session.IsActive, cancellationToken);
    }

    public async Task CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeleteAsync(ApplicationUser user, string? deletedBy, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        user.IsDeleted = true;
        user.IsActive = false;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = string.IsNullOrWhiteSpace(deletedBy) ? null : deletedBy.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task UpdateRolesAsync(ApplicationUser user, IList<string> roles, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                throw new InvalidOperationException($"Role '{role}' does not exist");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!removeResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await _userManager.AddToRolesAsync(user, roles);

        if (!addResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", addResult.Errors.Select(e => e.Description)));
    }
}