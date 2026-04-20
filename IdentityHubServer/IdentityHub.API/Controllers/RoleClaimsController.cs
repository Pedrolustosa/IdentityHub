using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleClaimsController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleClaimsController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        [HttpGet]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var roles = _roleManager.Roles.ToList();
            var permissions = new HashSet<string>();

            foreach (var role in roles)
            {
                var claims = await _roleManager.GetClaimsAsync(role);

                var rolePermissions = claims
                    .Where(c => c.Type == "permission")
                    .Select(c => c.Value);

                foreach (var permission in rolePermissions)
                {
                    permissions.Add(permission);
                }
            }

            return Ok(permissions.OrderBy(p => p));
        }

        [HttpGet("{roleId}")]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> GetByRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
                return NotFound("Role not found");

            var claims = await _roleManager.GetClaimsAsync(role);

            var permissions = claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value);

            return Ok(permissions);
        }

        [HttpPut("{roleId}")]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> UpdateRolePermissions(
            string roleId,
            [FromBody] List<string> permissions)
        {
            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
                return NotFound("Role not found");

            var currentClaims = await _roleManager.GetClaimsAsync(role);

            var currentPermissions = currentClaims
                .Where(c => c.Type == "permission")
                .ToList();

            foreach (var claim in currentPermissions)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            if (permissions != null && permissions.Any())
            {
                foreach (var permission in permissions.Distinct())
                {
                    await _roleManager.AddClaimAsync(
                        role,
                        new Claim("permission", permission));
                }
            }

            return Ok("Permissions updated");
        }
    }
}