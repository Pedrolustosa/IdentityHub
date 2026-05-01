using IdentityHub.Application.Common.Results;
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

    public Task<Result<List<string>>> GetPermissionsAsync(
        string roleId,
        CancellationToken cancellationToken)
        => _sender.Send(new GetRoleClaimPermissionsQuery(roleId), cancellationToken);

    public Task<Result> AddPermissionAsync(
        string roleId,
        string permission,
        CancellationToken cancellationToken)
        => _sender.Send(new AddRoleClaimPermissionCommand(roleId, permission), cancellationToken);

    public Task<Result> RemovePermissionAsync(
        string roleId,
        string permission,
        CancellationToken cancellationToken)
        => _sender.Send(new RemoveRoleClaimPermissionCommand(roleId, permission), cancellationToken);

    public Task<Result> ReplacePermissionsAsync(
        string roleId,
        List<string> permissions,
        CancellationToken cancellationToken)
        => _sender.Send(new ReplaceRoleClaimPermissionsCommand(roleId, permissions), cancellationToken);
}