using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.UserInvites.Commands;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.UserInvites.Handlers;

public sealed class CancelUserInviteCommandHandler : IRequestHandler<CancelUserInviteCommand, Result>
{
    private readonly IUserInvitesService _service;

    public CancelUserInviteCommandHandler(IUserInvitesService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(CancelUserInviteCommand command, CancellationToken cancellationToken)
    {
        return await _service.CancelUserInviteAsync(command.InviteId, cancellationToken);
    }
}
