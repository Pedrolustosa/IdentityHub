using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.UserInvites.Commands;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.UserInvites.Handlers;

public sealed class ResendUserInviteCommandHandler : IRequestHandler<ResendUserInviteCommand, Result>
{
    private readonly IUserInvitesService _service;

    public ResendUserInviteCommandHandler(IUserInvitesService service)
    {
        _service = service;
    }

    public async Task<Result> Handle(ResendUserInviteCommand command, CancellationToken cancellationToken)
    {
        return await _service.ResendUserInviteAsync(command.InviteId, cancellationToken);
    }
}
