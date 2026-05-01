using IdentityHub.Application.Common.Results;

namespace IdentityHub.Application.Interfaces;

public interface IRoleClaimService
{
    Task<Result<List<string>>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken);
    Task<Result> AddPermissionAsync(string roleId, string permission, CancellationToken cancellationToken);
    Task<Result> RemovePermissionAsync(string roleId, string permission, CancellationToken cancellationToken);
    Task<Result> ReplacePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken);
}