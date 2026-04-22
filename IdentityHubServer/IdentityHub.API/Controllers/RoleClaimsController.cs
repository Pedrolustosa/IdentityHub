using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/role-claims")]
public class RoleClaimsController : ControllerBase
{
    private readonly IRoleClaimService _service;

    public RoleClaimsController(IRoleClaimService service)
    {
        _service = service;
    }

    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetPermissions(string roleId)
        => Ok(await _service.GetPermissionsAsync(roleId));

    [HttpPost("{roleId}")]
    public async Task<IActionResult> AddPermission(string roleId, [FromBody] string permission)
    {
        await _service.AddPermissionAsync(roleId, permission);
        return Ok();
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> RemovePermission(string roleId, [FromQuery] string permission)
    {
        await _service.RemovePermissionAsync(roleId, permission);
        return Ok();
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> ReplacePermissions(
        string roleId,
        [FromBody] List<string> permissions)
    {
        await _service.ReplacePermissionsAsync(roleId, permissions);
        return Ok();
    }
}