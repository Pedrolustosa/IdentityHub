using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Roles.Handlers;

public sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result>
{
    private readonly IRoleRepository _repository;

    public UpdateRoleCommandHandler(IRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(
        UpdateRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Name))
            return Result.Failure(
                Error.Create("Role.NameRequired", "Role name is required"));

        var role = await _repository.GetByIdAsync(command.Id, cancellationToken);

        if (role is null)
            return Result.Failure(
                Error.Create("Role.NotFound", "Role not found"));

        var newName = command.Request.Name.Trim();

        var existingRole = await _repository.GetByNameAsync(newName, cancellationToken);

        if (existingRole is not null && existingRole.Id != role.Id)
            return Result.Failure(
                Error.Create("Role.AlreadyExists", "Another role with this name already exists"));

        role.Name = newName;
        role.NormalizedName = newName.ToUpperInvariant();

        await _repository.UpdateAsync(role, cancellationToken);

        return Result.Success();
    }
}