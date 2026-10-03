using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Constants;
using IdentityHub.Domain.Interfaces;
using MediatR;
using System.Security.Claims;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class UpdateRolePermissionsCommandHandler
    : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPermissionVersionService _permissionVersionService;

    public UpdateRolePermissionsCommandHandler(
        IRoleRepository repository,
        IAuditLogRepository auditLogRepository,
        IPermissionVersionService permissionVersionService)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _permissionVersionService = permissionVersionService;
    }

    public async Task<Result> Handle(
        UpdateRolePermissionsCommand command,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(command.RoleId, cancellationToken);

        if (role is null)
            return Result.Failure(
                Error.Create("Role.NotFound", "Role not found"));

        var permissions = command.Permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allowedPermissions = new HashSet<string>(AppPermissions.All(), StringComparer.OrdinalIgnoreCase);
        var invalidPermissions = permissions
            .Where(permission => !allowedPermissions.Contains(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToList();

        if (invalidPermissions.Count > 0)
            return Result.Failure(
                Error.Create(
                    "Role.InvalidPermission",
                    $"Unknown permission(s): {string.Join(", ", invalidPermissions)}"));

        var currentClaims = await _repository.GetClaimsAsync(role, cancellationToken);

        var currentPermissionClaims = currentClaims
            .Where(c => c.Type == PermissionClaimType)
            .ToList();

        var oldPermissions = currentPermissionClaims
            .Select(c => c.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var newPermissions = permissions
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
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

        await _permissionVersionService.BumpUsersInRoleAsync(role.Name, cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.Role.PermissionsUpdated",
            $"Role permissions updated: roleId={role.Id}, roleName={role.Name}, "
                + $"oldCount={oldPermissions.Count}, newCount={newPermissions.Count}, "
                + $"oldPermissions=[{string.Join(", ", oldPermissions)}], "
                + $"newPermissions=[{string.Join(", ", newPermissions)}]",
            role.Id,
            new
            {
                roleId = role.Id,
                roleName = role.Name,
                oldPermissions,
                newPermissions
            },
            cancellationToken);

        return Result.Success();
    }
}
