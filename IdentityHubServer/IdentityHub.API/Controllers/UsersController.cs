using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 🔹 LISTAR TODOS
        [HttpGet]
        [Authorize(Policy = "Users.View")]
        public async Task<IActionResult> GetAll()
        {
            var users = _userManager.Users.ToList();

            var result = new List<UserResponse>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    IsActive = user.IsActive,
                    Roles = roles
                });
            }

            return Ok(result);
        }

        // 🔹 GET BY ID
        [HttpGet("{id}")]
        [Authorize(Policy = "Users.View")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = roles
            });
        }

        // 🔹 CREATE USER
        [HttpPost]
        [Authorize(Policy = "Users.Create")]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Email and Password are required");
            }

            var user = new ApplicationUser
            {
                UserName = request.Email.Trim(),
                Email = request.Email.Trim(),
                FullName = request.FullName?.Trim(),
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // 🔹 default role
            if (await _roleManager.RoleExistsAsync("User"))
            {
                await _userManager.AddToRoleAsync(user, "User");
            }

            return Ok("User created");
        }

        // 🔹 UPDATE USER
        [HttpPut("{id}")]
        [Authorize(Policy = "Users.Update")]
        public async Task<IActionResult> Update(string id, UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.FullName = request.FullName?.Trim();
            user.IsActive = request.IsActive;

            await _userManager.UpdateAsync(user);

            return Ok("User updated");
        }

        // 🔹 DELETE USER
        [HttpDelete("{id}")]
        [Authorize(Policy = "Users.Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            await _userManager.DeleteAsync(user);

            return Ok("User deleted");
        }

        // 🔹 UPDATE ROLES
        [HttpPut("{id}/roles")]
        [Authorize(Policy = "Roles.Manage")]
        public async Task<IActionResult> UpdateRoles(string id, UpdateRolesRequest request)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // 🔒 valida roles existentes
            var invalidRoles = new List<string>();

            foreach (var role in request.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    invalidRoles.Add(role);
                }
            }

            if (invalidRoles.Any())
            {
                return BadRequest(new
                {
                    message = "Invalid roles",
                    invalidRoles
                });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRolesAsync(user, request.Roles);

            return Ok("Roles updated");
        }
    }
}