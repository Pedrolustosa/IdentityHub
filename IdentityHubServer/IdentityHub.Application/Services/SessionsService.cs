using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Sessions.Commands;
using IdentityHub.Application.CQRS.Sessions.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using MediatR;

namespace IdentityHub.Application.Services;

public sealed class SessionsService : ISessionsService
{
    private readonly ISender _sender;

    public SessionsService(ISender sender)
    {
        _sender = sender;
    }

    public Task<Result<PagedResponse<AdminSessionResponse>>> GetPagedAsync(
        SessionFilter filter,
        int page,
        int pageSize,
        Guid? currentSessionId,
        CancellationToken cancellationToken = default)
        => _sender.Send(new GetSystemSessionsQuery(filter, page, pageSize, currentSessionId), cancellationToken);

    public Task<Result> RevokeAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => _sender.Send(new RevokeSystemSessionCommand(sessionId), cancellationToken);
}
