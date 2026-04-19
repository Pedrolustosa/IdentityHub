using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 GET ALL
        [HttpGet]
        [Authorize(Policy = "Permissions.View")]
        public async Task<IActionResult> GetAll()
        {
            var permissions = await _context.Permissions
                .Select(p => new PermissionResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description
                })
                .ToListAsync();

            return Ok(permissions);
        }

        // 🔹 GET BY ID
        [HttpGet("{id}")]
        [Authorize(Policy = "Permissions.View")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var permission = await _context.Permissions.FindAsync(id);

            if (permission == null)
                return NotFound();

            return Ok(new PermissionResponse
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description
            });
        }

        // 🔹 CREATE
        [HttpPost]
        [Authorize(Policy = "Permissions.Manage")]
        public async Task<IActionResult> Create(CreatePermissionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required");

            var name = request.Name.Trim();

            var exists = await _context.Permissions
                .AnyAsync(p => p.Name == name);

            if (exists)
                return BadRequest("Permission already exists");

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = request.Description?.Trim()
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            return Ok(new PermissionResponse
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description
            });
        }

        // 🔹 UPDATE
        [HttpPut("{id}")]
        [Authorize(Policy = "Permissions.Manage")]
        public async Task<IActionResult> Update(Guid id, UpdatePermissionRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request");

            var permission = await _context.Permissions.FindAsync(id);

            if (permission == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(request.Name) && request.Description == null)
                return BadRequest("Nothing to update");

            // 🔹 NAME
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var newName = request.Name.Trim();

                var exists = await _context.Permissions
                    .AnyAsync(p => p.Name == newName && p.Id != id);

                if (exists)
                    return BadRequest("Another permission with this name already exists");

                permission.Name = newName;
            }

            // 🔹 DESCRIPTION
            if (request.Description != null)
            {
                permission.Description = request.Description.Trim();
            }

            await _context.SaveChangesAsync();

            return Ok(new PermissionResponse
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description
            });
        }

        // 🔹 DELETE
        [HttpDelete("{id}")]
        [Authorize(Policy = "Permissions.Manage")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var permission = await _context.Permissions.FindAsync(id);

            if (permission == null)
                return NotFound();

            // ⚠️ FUTURO: validar uso em roles/users
            // (isso evita 403 depois de deletar)

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            return Ok("Permission deleted successfully");
        }
    }
}