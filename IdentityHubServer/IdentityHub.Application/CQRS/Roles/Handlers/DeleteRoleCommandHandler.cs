using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly IRoleRepository _repository;

    public DeleteRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        DeleteRoleCommand command,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (role is null)
            throw new InvalidOperationException("Role not found");

        if (role.Name is "Admin")
            throw new InvalidOperationException("Admin role cannot be deleted");

        await _repository.DeleteAsync(role, cancellationToken);
    }
}