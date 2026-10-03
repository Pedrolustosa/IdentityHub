using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IRoleRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPermissionVersionService _permissionVersionService;

    public DeleteRoleCommandHandler(
        IRoleRepository repository,
        IAuditLogRepository auditLogRepository,
        IPermissionVersionService permissionVersionService)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
        _permissionVersionService = permissionVersionService;
    }

    public async Task<Result> Handle(
        DeleteRoleCommand command,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (role is null)
            return Result.Failure(
                Error.Create("Role.NotFound", "Role not found"));

        if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return Result.Failure(
                Error.Create("Role.AdminCannotBeDeleted", "Admin role cannot be deleted"));

        var roleId = role.Id;
        var roleName = role.Name ?? string.Empty;

        // Invalidate JWTs for members before the role (and its claims) disappears.
        await _permissionVersionService.BumpUsersInRoleAsync(role.Name, cancellationToken);

        await _repository.DeleteAsync(role, cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.Role.Deleted",
            $"Role deleted: id={roleId}, name={roleName}",
            cancellationToken);

        return Result.Success();
    }
}
