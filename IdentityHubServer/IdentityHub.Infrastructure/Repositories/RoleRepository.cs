using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdentityHub.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _context;

    public RoleRepository(
        RoleManager<IdentityRole> roleManager,
        AppDbContext context)
    {
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<List<IdentityRole>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IdentityRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _roleManager.FindByIdAsync(id);
    }

    public async Task<IdentityRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _roleManager.FindByNameAsync(name);
    }

    public async Task<int> GetUserCountAsync(string roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _context.Set<IdentityUserRole<string>>()
            .AsNoTracking()
            .CountAsync(ur => ur.RoleId == roleId, cancellationToken);
    }

    public async Task<IDictionary<string, int>> GetUserCountsByRoleIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _context.Set<IdentityUserRole<string>>()
            .AsNoTracking()
            .GroupBy(ur => ur.RoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);
    }

    public async Task CreateAsync(IdentityRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task UpdateAsync(IdentityRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeleteAsync(IdentityRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _roleManager.GetClaimsAsync(role);
    }

    public async Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _roleManager.AddClaimAsync(role, claim);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _roleManager.RemoveClaimAsync(role, claim);

        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}