using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IRoleRepository _repository;

    public DeleteRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
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

        await _repository.DeleteAsync(role, cancellationToken);

        return Result.Success();
    }
}