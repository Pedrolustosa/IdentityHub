using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand>
{
    private readonly IRoleRepository _repository;

    public UpdateRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Name))
            throw new InvalidOperationException("Role name is required");

        var role = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (role is null)
            throw new InvalidOperationException("Role not found");

        var newName = command.Request.Name.Trim();

        var existingRole = await _repository.GetByNameAsync(newName, cancellationToken);

        if (existingRole is not null && existingRole.Id != role.Id)
            throw new InvalidOperationException("Another role with this name already exists");

        role.Name = newName;
        role.NormalizedName = newName.ToUpperInvariant();

        await _repository.UpdateAsync(role, cancellationToken);
    }
}