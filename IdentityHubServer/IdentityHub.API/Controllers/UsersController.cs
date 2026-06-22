using IdentityHub.API.Extensions;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(
    IUserService service,
    IAuthService authService,
    IAuditLogService auditLogService) : ControllerBase
{
    private readonly IUserService _service = service;
    private readonly IAuthService _authService = authService;
    private readonly IAuditLogService _auditLogService = auditLogService;

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

    [HttpPost("invite")]
    [Authorize(Policy = "UserInvites.Create")]
    public async Task<IActionResult> Invite(
        InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.InviteAsync(request, cancellationToken);
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

    [HttpGet("{id}/sessions")]
    [Authorize(Policy = "Users.View")]
    public async Task<IActionResult> GetSessionsByUser(
        string id,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        Guid? currentSessionId = null;

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.Equals(currentUserId, id, StringComparison.OrdinalIgnoreCase))
        {
            var sidValue = User.FindFirst("sid")?.Value;
            if (Guid.TryParse(sidValue, out var parsedSessionId))
            {
                currentSessionId = parsedSessionId;
            }
        }

        var result = await _authService.GetRecentSessionsAsync(id, currentSessionId, take, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id}/sessions/{sessionId:guid}")]
    [Authorize(Policy = "Sessions.Revoke")]
    public async Task<IActionResult> RevokeUserSession(
        string id,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.RevokeSessionAsync(id, sessionId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id}/audit-logs")]
    [Authorize(Policy = "Audit.View")]
    public async Task<IActionResult> GetAuditLogsByUser(
        string id,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _auditLogService.GetRecentByUserAsync(id, take, cancellationToken);
        return result.ToActionResult();
    }
}