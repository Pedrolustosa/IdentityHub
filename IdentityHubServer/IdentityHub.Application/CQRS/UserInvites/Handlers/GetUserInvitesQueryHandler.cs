using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.UserInvites.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.UserInvites.Handlers;

public sealed class GetUserInvitesQueryHandler : IRequestHandler<GetUserInvitesQuery, Result<UserInvitesPagedResponse>>
{
    private readonly IUserInvitesService _service;

    public GetUserInvitesQueryHandler(IUserInvitesService service)
    {
        _service = service;
    }

    public async Task<Result<UserInvitesPagedResponse>> Handle(GetUserInvitesQuery query, CancellationToken cancellationToken)
    {
        return await _service.GetUserInvitesAsync(query.Page, query.PageSize, cancellationToken);
    }
}
