using IdentityHub.API.Extensions;
using IdentityHub.Application.CQRS.SecuritySettings.Commands;
using IdentityHub.Application.CQRS.SecuritySettings.Queries;
using IdentityHub.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/security-settings")]
[Authorize]
public sealed class SecuritySettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SecuritySettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "SecuritySettings.View")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var query = new GetSecuritySettingsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut]
    [Authorize(Policy = "SecuritySettings.Update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateSecuritySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            return BadRequest("Settings request is required");

        var command = new UpdateSecuritySettingsCommand(request);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
