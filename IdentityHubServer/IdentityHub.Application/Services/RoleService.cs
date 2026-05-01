using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Application.CQRS.Roles.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.Services;

public sealed class RoleService : IRoleService
{
    private readonly ISender _sender;

    public RoleService(ISender sender)
    {
        _sender = sender;
    }

    public Task<List<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        => _sender.Send(new GetRolesQuery(), cancellationToken);

    public Task<RoleResponse?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _sender.Send(new GetRoleByIdQuery(id), cancellationToken);

    public Task CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        => _sender.Send(new CreateRoleCommand(request), cancellationToken);

    public Task UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
        => _sender.Send(new UpdateRoleCommand(id, request), cancellationToken);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _sender.Send(new DeleteRoleCommand(id), cancellationToken);

    public Task<List<string>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken = default)
        => _sender.Send(new GetRolePermissionsQuery(roleId), cancellationToken);

    public Task UpdatePermissionsAsync(
        string roleId,
        List<string> permissions,
        CancellationToken cancellationToken = default)
        => _sender.Send(new UpdateRolePermissionsCommand(roleId, permissions), cancellationToken);
}