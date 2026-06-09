using IdentityHub.Application.Common.Results;
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

    public Task<Result<List<RoleResponse>>> GetAllAsync(CancellationToken cancellationToken)
        => _sender.Send(new GetRolesQuery(), cancellationToken);

    public Task<Result<RoleResponse>> GetByIdAsync(string id, CancellationToken cancellationToken)
        => _sender.Send(new GetRoleByIdQuery(id), cancellationToken);

    public Task<Result> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
        => _sender.Send(new CreateRoleCommand(request), cancellationToken);

    public Task<Result> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken)
        => _sender.Send(new UpdateRoleCommand(id, request), cancellationToken);

    public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken)
        => _sender.Send(new DeleteRoleCommand(id), cancellationToken);

    public Task<Result<List<string>>> GetPermissionCatalogAsync(CancellationToken cancellationToken)
        => _sender.Send(new GetPermissionCatalogQuery(), cancellationToken);

    public Task<Result<List<string>>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken)
        => _sender.Send(new GetRolePermissionsQuery(roleId), cancellationToken);

    public Task<Result> UpdatePermissionsAsync(
        string roleId,
        List<string> permissions,
        CancellationToken cancellationToken)
        => _sender.Send(new UpdateRolePermissionsCommand(roleId, permissions), cancellationToken);
}