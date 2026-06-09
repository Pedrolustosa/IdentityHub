using IdentityHub.API.Extensions;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _service;

    public RolesController(IRoleService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = "Roles.View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Roles.View")]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = "Roles.Create")]
    public async Task<IActionResult> Create(
        CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Roles.Update")]
    public async Task<IActionResult> Update(
        string id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Roles.Delete")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("permissions/catalog")]
    [Authorize(Policy = "Roles.Permissions.View")]
    public async Task<IActionResult> GetPermissionCatalog(CancellationToken cancellationToken)
    {
        var result = await _service.GetPermissionCatalogAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id}/permissions")]
    [Authorize(Policy = "Roles.Permissions.View")]
    public async Task<IActionResult> GetPermissions(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetPermissionsAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id}/permissions")]
    [Authorize(Policy = "Roles.Permissions.Update")]
    public async Task<IActionResult> UpdatePermissions(
        string id,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdatePermissionsAsync(
            id,
            request.Permissions,
            cancellationToken);

        return result.ToActionResult();
    }
}