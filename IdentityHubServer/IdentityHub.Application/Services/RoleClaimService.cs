using IdentityHub.Application.CQRS.RoleClaims.Commands;
using IdentityHub.Application.CQRS.RoleClaims.Queries;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.Services;

public sealed class RoleClaimService : IRoleClaimService
{
    private readonly ISender _sender;

    public RoleClaimService(ISender sender)
    {
        _sender = sender;
    }

    public Task<List<string>> GetPermissionsAsync(
        string roleId,
        CancellationToken cancellationToken = default)
        => _sender.Send(new GetRoleClaimPermissionsQuery(roleId), cancellationToken);

    public Task AddPermissionAsync(
        string roleId,
        string permission,
        CancellationToken cancellationToken = default)
        => _sender.Send(new AddRoleClaimPermissionCommand(roleId, permission), cancellationToken);

    public Task RemovePermissionAsync(
        string roleId,
        string permission,
        CancellationToken cancellationToken = default)
        => _sender.Send(new RemoveRoleClaimPermissionCommand(roleId, permission), cancellationToken);

    public Task ReplacePermissionsAsync(
        string roleId,
        List<string> permissions,
        CancellationToken cancellationToken = default)
        => _sender.Send(new ReplaceRoleClaimPermissionsCommand(roleId, permissions), cancellationToken);
}