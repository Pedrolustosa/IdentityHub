using IdentityHub.Application.CQRS.RoleClaims.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;
using System.Security.Claims;

namespace IdentityHub.Application.CQRS.RoleClaims.Handlers;

public sealed class AddRoleClaimPermissionCommandHandler
    : IRequestHandler<AddRoleClaimPermissionCommand>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;

    public AddRoleClaimPermissionCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        AddRoleClaimPermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Permission))
            throw new InvalidOperationException("Permission is required");

        var role = await _repository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
            throw new InvalidOperationException("Role not found");

        var permission = request.Permission.Trim();

        var claims = await _repository.GetClaimsAsync(role, cancellationToken);

        var exists = claims.Any(c =>
            c.Type == PermissionClaimType &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));

        if (exists)
            throw new InvalidOperationException("Permission already exists");

        await _repository.AddClaimAsync(
            role,
            new Claim(PermissionClaimType, permission),
            cancellationToken);
    }
}