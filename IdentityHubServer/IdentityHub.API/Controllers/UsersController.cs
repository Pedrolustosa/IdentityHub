using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
            => Ok(await _service.GetAllAsync(cancellationToken));

        [HttpGet("{id}")]
        [Authorize(Policy = "Users.View")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
        {
            var user = await _service.GetByIdAsync(id, cancellationToken);

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        [HttpPost]
        [Authorize(Policy = "Users.Create")]
        public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
        {
            await _service.CreateAsync(request, cancellationToken);
            return Ok();
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Users.Update")]
        public async Task<IActionResult> Update(string id, UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _service.UpdateAsync(id, request, actingUserId, cancellationToken);
            return Ok();
        }

        [HttpPut("{id}/roles")]
        [Authorize(Policy = "Users.Roles.Update")]
        public async Task<IActionResult> UpdateRoles(string id, UpdateRolesRequest request, CancellationToken cancellationToken)
        {
            await _service.UpdateRolesAsync(id, request, cancellationToken);
            return Ok();
        }
    }
}
