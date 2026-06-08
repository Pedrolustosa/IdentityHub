using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.RoleClaims.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.RoleClaims.Handlers;

public sealed class RemoveRoleClaimPermissionCommandHandler
    : IRequestHandler<RemoveRoleClaimPermissionCommand, Result>
{
    private const string PermissionClaimType = "permission";
    private readonly IRoleRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public RemoveRoleClaimPermissionCommandHandler(
        IRoleRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result> Handle(
        RemoveRoleClaimPermissionCommand request,
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

        var claim = claims.FirstOrDefault(c =>
            c.Type == PermissionClaimType &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));

        if (claim is null)
            return Result.Success();

        await _repository.RemoveClaimAsync(role, claim, cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.RoleClaim.Removed",
            $"Role claim removed: roleId={role.Id}, roleName={role.Name}, permission={permission}",
            cancellationToken);

        return Result.Success();
    }
}