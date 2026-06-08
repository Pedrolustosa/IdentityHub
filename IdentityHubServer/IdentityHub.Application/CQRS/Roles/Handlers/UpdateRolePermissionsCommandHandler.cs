using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Commands;
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

    public UpdateRolePermissionsCommandHandler(
        IRoleRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
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

        await _auditLogRepository.WriteAsync(
            "Audit.Role.PermissionsUpdated",
            $"Role permissions updated: roleId={role.Id}, roleName={role.Name}, count={permissions.Count}",
            cancellationToken);

        return Result.Success();
    }
}