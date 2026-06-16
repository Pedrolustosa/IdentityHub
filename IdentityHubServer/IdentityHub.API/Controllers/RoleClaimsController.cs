using IdentityHub.API.Extensions;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/role-claims")]
[Authorize]
public sealed class RoleClaimsController : ControllerBase
{
    private readonly IRoleClaimService _service;

    public RoleClaimsController(IRoleClaimService service)
    {
        _service = service;
    }

    [HttpGet("{roleId}")]
    [Authorize(Policy = "Roles.Permissions.View")]
    public async Task<IActionResult> GetPermissions(
        string roleId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetPermissionsAsync(roleId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{roleId}")]
    [Authorize(Policy = "Roles.Permissions.Update")]
    public async Task<IActionResult> AddPermission(
        string roleId,
        [FromBody] string permission,
        CancellationToken cancellationToken)
    {
        var result = await _service.AddPermissionAsync(roleId, permission, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{roleId}")]
    [Authorize(Policy = "Roles.Permissions.Update")]
    public async Task<IActionResult> RemovePermission(
        string roleId,
        [FromQuery] string permission,
        CancellationToken cancellationToken)
    {
        var result = await _service.RemovePermissionAsync(roleId, permission, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{roleId}")]
    [Authorize(Policy = "Roles.Permissions.Update")]
    public async Task<IActionResult> ReplacePermissions(
        string roleId,
        [FromBody] List<string> permissions,
        CancellationToken cancellationToken)
    {
        var result = await _service.ReplacePermissionsAsync(
            roleId,
            permissions,
            cancellationToken);

        return result.ToActionResult();
    }
}