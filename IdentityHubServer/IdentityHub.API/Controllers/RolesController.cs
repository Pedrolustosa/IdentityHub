using IdentityHub.Application.DTOs;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public RolesController(
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        [Authorize(Policy = "Roles.View")]
        public IActionResult GetAll()
        {
            var roles = _roleManager.Roles
                .Select(r => new RoleResponse
                {
                    Id = r.Id,
                    Name = r.Name
                })
                .ToList();

            return Ok(roles);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "Roles.View")]
        public async Task<IActionResult> GetById(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            return Ok(new RoleResponse
            {
                Id = role.Id,
                Name = role.Name
            });
        }

        [HttpPost]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> Create(CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Role name is required");

            var name = request.Name.Trim();

            var exists = await _roleManager.RoleExistsAsync(name);

            if (exists)
                return BadRequest("Role already exists");

            var result = await _roleManager.CreateAsync(
                new IdentityRole(name));

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Role created");
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> Update(string id, UpdateRoleRequest request)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Role name is required");

            var newName = request.Name.Trim();

            var exists = await _roleManager.Roles
                .AnyAsync(r => r.Name == newName && r.Id != id);

            if (exists)
                return BadRequest("Another role with this name already exists");

            role.Name = newName;

            var result = await _roleManager.UpdateAsync(role);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Role updated");
        }

        [HttpGet("{id}/permissions")]
        [Authorize(Policy = "Roles.View")]
        public async Task<IActionResult> GetPermissions(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            var claims = await _roleManager.GetClaimsAsync(role);

            var permissions = claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

            return Ok(permissions);
        }

        [HttpPut("{id}/permissions")]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> UpdatePermissions(
            string id,
            UpdateRolePermissionsRequest request)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            if (request.Permissions == null || !request.Permissions.Any())
                return BadRequest("Permissions are required");

            var currentClaims = await _roleManager.GetClaimsAsync(role);

            var currentPermissions = currentClaims
                .Where(c => c.Type == "permission")
                .ToList();

            foreach (var claim in currentPermissions)
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var permission in request.Permissions.Distinct())
            {
                await _roleManager.AddClaimAsync(
                    role,
                    new Claim("permission", permission.Trim()));
            }

            return Ok("Permissions updated");
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                return NotFound();

            var result = await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Role deleted");
        }
    }
}