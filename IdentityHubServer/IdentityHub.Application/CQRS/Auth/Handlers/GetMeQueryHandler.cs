using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class GetMeQueryHandler : IRequestHandler<GetMeQuery, Result<MeResponse>>
{
    private const string PermissionClaimType = "permission";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public GetMeQueryHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<MeResponse>> Handle(
        GetMeQuery query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(query.UserId);

        if (user is null || user.IsDeleted)
            return Result<MeResponse>.Failure(
                Error.Create("User.NotFound", "User not found"));

        var roles = await _userManager.GetRolesAsync(user);

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var role = await _roleManager.FindByNameAsync(roleName);

            if (role is null)
                continue;

            var roleClaims = await _roleManager.GetClaimsAsync(role);

            foreach (var claim in roleClaims.Where(c => c.Type == PermissionClaimType))
                permissions.Add(claim.Value);
        }

        var userClaims = await _userManager.GetClaimsAsync(user);
        foreach (var claim in userClaims.Where(c => c.Type == PermissionClaimType))
            permissions.Add(claim.Value);

        return Result<MeResponse>.Success(new MeResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles,
            Permissions = permissions.OrderBy(x => x).ToList()
        });
    }
}
