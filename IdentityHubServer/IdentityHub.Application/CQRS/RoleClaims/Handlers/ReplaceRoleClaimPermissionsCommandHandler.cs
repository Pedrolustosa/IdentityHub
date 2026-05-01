using IdentityHub.Application.CQRS.RoleClaims.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;
using System.Security.Claims;

namespace IdentityHub.Application.CQRS.RoleClaims.Handlers;

public sealed class ReplaceRoleClaimPermissionsCommandHandler
    : IRequestHandler<ReplaceRoleClaimPermissionsCommand>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;

    public ReplaceRoleClaimPermissionsCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        ReplaceRoleClaimPermissionsCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
            throw new InvalidOperationException("Role not found");

        var permissions = request.Permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentClaims = await _repository.GetClaimsAsync(role, cancellationToken);

        var currentPermissionClaims = currentClaims
            .Where(c => c.Type == PermissionClaimType)
            .ToList();

        foreach (var claim in currentPermissionClaims)
            await _repository.RemoveClaimAsync(role, claim, cancellationToken);

        foreach (var permission in permissions)
        {
            await _repository.AddClaimAsync(
                role,
                new Claim(PermissionClaimType, permission),
                cancellationToken);
        }
    }
}