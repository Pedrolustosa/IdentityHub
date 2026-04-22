using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController(IUserService service) : ControllerBase
    {
        private readonly IUserService _service = service;

        [HttpGet]
        [Authorize(Policy = "Users.View")]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        [Authorize(Policy = "Users.View")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _service.GetByIdAsync(id);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        [Authorize(Policy = "Users.Create")]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            await _service.CreateAsync(request);
            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Users.Update")]
        public async Task<IActionResult> Update(string id, UpdateUserRequest request)
        {
            await _service.UpdateAsync(id, request);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "Users.Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            await _service.DeleteAsync(id);
            return Ok();
        }

        [HttpPut("{id}/roles")]
        [Authorize(Policy = "Users.Roles.Update")]
        public async Task<IActionResult> UpdateRoles(string id, UpdateRolesRequest request)
        {
            await _service.UpdateRolesAsync(id, request);
            return Ok();
        }
    }
}