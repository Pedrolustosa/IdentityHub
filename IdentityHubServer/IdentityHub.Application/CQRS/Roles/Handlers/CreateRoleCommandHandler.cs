using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand>
{
    private readonly IRoleRepository _repository;

    public CreateRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Name))
            throw new InvalidOperationException("Role name is required");

        var name = command.Request.Name.Trim();

        var existingRole = await _repository.GetByNameAsync(name, cancellationToken);

        if (existingRole is not null)
            throw new InvalidOperationException("Role already exists");

        await _repository.CreateAsync(new IdentityRole(name), cancellationToken);
    }
}