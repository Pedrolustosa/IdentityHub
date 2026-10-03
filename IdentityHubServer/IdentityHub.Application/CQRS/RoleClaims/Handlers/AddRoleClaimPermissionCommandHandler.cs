using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.RoleClaims.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;
using MediatR;
using System.Security.Claims;

namespace IdentityHub.Application.CQRS.RoleClaims.Handlers;

public sealed class AddRoleClaimPermissionCommandHandler
    : IRequestHandler<AddRoleClaimPermissionCommand, Result>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPermissionVersionService _permissionVersionService;

    public AddRoleClaimPermissionCommandHandler(
        IRoleRepository repository,
        IAuditLogRepository auditLogRepository,
        IPermissionVersionService permissionVersionService)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _permissionVersionService = permissionVersionService;
    }

    public async Task<Result> Handle(
        AddRoleClaimPermissionCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Permission))
            return Result.Failure(
                Error.Create("RoleClaim.PermissionRequired", "Permission is required"));

        var role = await _repository.GetByIdAsync(request.RoleId, cancellationToken);

        if (role is null)
            return Result.Failure(
                Error.Create("Role.NotFound", "Role not found"));

        var permission = request.Permission.Trim();

        var claims = await _repository.GetClaimsAsync(role, cancellationToken);

        var exists = claims.Any(c =>
            c.Type == PermissionClaimType &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));

        if (exists)
            return Result.Failure(
                Error.Create("RoleClaim.AlreadyExists", "Permission already exists"));

        await _repository.AddClaimAsync(
            role,
            new Claim(PermissionClaimType, permission),
            cancellationToken);

        await _permissionVersionService.BumpUsersInRoleAsync(role.Name, cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.RoleClaim.Added",
            $"Role claim added: roleId={role.Id}, roleName={role.Name}, permission={permission}",
            cancellationToken);

        return Result.Success();
    }
}
