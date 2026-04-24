using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Roles.View")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _service;

        public RolesController(IRoleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var role = await _service.GetByIdAsync(id);

            if (role == null)
                return NotFound();

            return Ok(role);
        }

        [HttpPost]
        [Authorize(Policy = "Roles.Create")]
        public async Task<IActionResult> Create(CreateRoleRequest request)
        {
            await _service.CreateAsync(request);
            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Roles.Update")]
        public async Task<IActionResult> Update(string id, UpdateRoleRequest request)
        {
            await _service.UpdateAsync(id, request);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Roles.Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteAsync(id);
            return Ok();
        }

        [HttpGet("{id}/permissions")]
        [Authorize(Policy = "Roles.Permissions.View")]
        public async Task<IActionResult> GetPermissions(string id)
            => Ok(await _service.GetPermissionsAsync(id));

        [HttpPut("{id}/permissions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePermissions(
            string id,
            UpdateRolePermissionsRequest request)
        {
            await _service.UpdatePermissionsAsync(id, request.Permissions);
            return Ok();
        }
    }
}