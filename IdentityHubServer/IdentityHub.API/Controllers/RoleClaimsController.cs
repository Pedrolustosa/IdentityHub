using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/role-claims")]
    [Authorize]
    public class RoleClaimsController : ControllerBase
    {
        private readonly IRoleClaimService _service;

        public RoleClaimsController(IRoleClaimService service)
        {
            _service = service;
        }

        [HttpGet("{roleId}")]
        [Authorize(Policy = "RoleClaims.View")]
        public async Task<IActionResult> GetPermissions(string roleId)
            => Ok(await _service.GetPermissionsAsync(roleId));

        [HttpPost("{roleId}")]
        [Authorize(Policy = "RoleClaims.Manage")]
        public async Task<IActionResult> AddPermission(string roleId, [FromBody] string permission)
        {
            await _service.AddPermissionAsync(roleId, permission);
            return Ok();
        }

        [HttpDelete("{roleId}")]
        [Authorize(Policy = "RoleClaims.Manage")]
        public async Task<IActionResult> RemovePermission(string roleId, [FromQuery] string permission)
        {
            await _service.RemovePermissionAsync(roleId, permission);
            return Ok();
        }

        [HttpPut("{roleId}")]
        [Authorize(Policy = "RoleClaims.Manage")]
        public async Task<IActionResult> ReplacePermissions(
            string roleId,
            [FromBody] List<string> permissions)
        {
            await _service.ReplacePermissionsAsync(roleId, permissions);
            return Ok();
        }
    }
}