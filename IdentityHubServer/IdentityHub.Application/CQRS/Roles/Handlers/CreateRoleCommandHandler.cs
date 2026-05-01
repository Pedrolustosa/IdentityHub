using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result>
{
    private readonly IRoleRepository _repository;

    public CreateRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(
        CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Name))
            return Result.Failure(
                Error.Create("Role.NameRequired", "Role name is required"));

        var name = command.Request.Name.Trim();

        var existingRole = await _repository.GetByNameAsync(name, cancellationToken);

        if (existingRole is not null)
            return Result.Failure(
                Error.Create("Role.AlreadyExists", "Role already exists"));

        await _repository.CreateAsync(new IdentityRole(name), cancellationToken);

        return Result.Success();
    }
}