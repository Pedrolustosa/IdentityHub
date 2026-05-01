using IdentityHub.API.Extensions;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IUserService service) : ControllerBase
{
    private readonly IUserService _service = service;

    [HttpGet]
    [Authorize(Policy = "Users.View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "Users.View")]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [Authorize(Policy = "Users.Create")]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Users.Update")]
    public async Task<IActionResult> Update(
        string id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Users.Delete")]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id}/roles")]
    [Authorize(Policy = "Users.Roles.Update")]
    public async Task<IActionResult> UpdateRoles(
        string id,
        UpdateRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateRolesAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}