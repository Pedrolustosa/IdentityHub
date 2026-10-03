using IdentityHub.API.Extensions;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize(Policy = "Sessions.View")]
public sealed class SessionsController : ControllerBase
{
    private readonly ISessionsService _service;

    public SessionsController(ISessionsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] SessionFilter filter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Guid? currentSessionId = null;
        var sidValue = User.FindFirst("sid")?.Value;
        if (Guid.TryParse(sidValue, out var parsedSessionId))
            currentSessionId = parsedSessionId;

        var result = await _service.GetPagedAsync(filter, page, pageSize, currentSessionId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{sessionId:guid}")]
    [Authorize(Policy = "Sessions.Revoke")]
    public async Task<IActionResult> Revoke(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.RevokeAsync(sessionId, cancellationToken);
        return result.ToActionResult();
    }
}
