using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityHub.Application.Interfaces
{
    public interface IRoleClaimService
    {
        Task<List<string>> GetPermissionsAsync(string roleId);
        Task AddPermissionAsync(string roleId, string permission);
        Task RemovePermissionAsync(string roleId, string permission);
        Task ReplacePermissionsAsync(string roleId, List<string> permissions);
    }
}
