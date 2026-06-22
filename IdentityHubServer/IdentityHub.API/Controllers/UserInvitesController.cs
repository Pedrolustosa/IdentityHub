using IdentityHub.API.Extensions;
using IdentityHub.Application.CQRS.UserInvites.Commands;
using IdentityHub.Application.CQRS.UserInvites.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityHub.API.Controllers;

[ApiController]
[Route("api/user-invites")]
[Authorize]
public sealed class UserInvitesController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserInvitesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "UserInvites.View")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserInvitesQuery(page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id}/resend")]
    [Authorize(Policy = "UserInvites.Resend")]
    public async Task<IActionResult> ResendInvite(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var inviteId))
            return BadRequest("Invalid invite ID format");

        var command = new ResendUserInviteCommand(inviteId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "UserInvites.Cancel")]
    public async Task<IActionResult> CancelInvite(
        string id,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(id, out var inviteId))
            return BadRequest("Invalid invite ID format");

        var command = new CancelUserInviteCommand(inviteId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
