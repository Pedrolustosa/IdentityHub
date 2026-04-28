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
        public async Task<IActionResult> GetPermissions(string roleId, CancellationToken cancellationToken)
            => Ok(await _service.GetPermissionsAsync(roleId, cancellationToken));

        [HttpPost("{roleId}")]
        [Authorize(Policy = "RoleClaims.Manage")]
        public async Task<IActionResult> AddPermission(string roleId, [FromBody] string permission, CancellationToken cancellationToken)
        {
            await _service.AddPermissionAsync(roleId, permission, cancellationToken);
            return Ok();
        }

        [HttpDelete("{roleId}")]
        [Authorize(Policy = "RoleClaims.Manage")]
        public async Task<IActionResult> RemovePermission(string roleId, [FromQuery] string permission, CancellationToken cancellationToken)
        {
            await _service.RemovePermissionAsync(roleId, permission, cancellationToken);
            return Ok();
        }

        [HttpPut("{roleId}")]
        [Authorize(Policy = "RoleClaims.Manage")]
        public async Task<IActionResult> ReplacePermissions(
            string roleId,
            [FromBody] List<string> permissions,
            CancellationToken cancellationToken)
        {
            await _service.ReplacePermissionsAsync(roleId, permissions, cancellationToken);
            return Ok();
        }
    }
}
