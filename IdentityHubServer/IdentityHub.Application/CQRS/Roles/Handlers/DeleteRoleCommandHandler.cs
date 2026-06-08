using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IRoleRepository _repository;
    private readonly IAuditLogRepository _auditLogRepository;

    public DeleteRoleCommandHandler(
        IRoleRepository repository,
        IAuditLogRepository auditLogRepository)
    {
        _repository = repository;
        _auditLogRepository = auditLogRepository;
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

        await _repository.DeleteAsync(role, cancellationToken);

        await _auditLogRepository.WriteAsync(
            "Audit.Role.Deleted",
            $"Role deleted: id={roleId}, name={roleName}",
            cancellationToken);

        return Result.Success();
    }
}