namespace IdentityHub.Application.Interfaces;

public interface IRoleClaimService
{
    Task<List<string>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken = default);
    Task AddPermissionAsync(string roleId, string permission, CancellationToken cancellationToken = default);
    Task RemovePermissionAsync(string roleId, string permission, CancellationToken cancellationToken = default);
    Task ReplacePermissionsAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default);
}