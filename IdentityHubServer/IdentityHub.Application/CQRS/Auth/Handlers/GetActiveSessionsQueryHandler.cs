using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class GetActiveSessionsQueryHandler : IRequestHandler<GetActiveSessionsQuery, Result<IReadOnlyList<UserSessionResponse>>>
{
    private readonly IAuthRepository _repo;

    public GetActiveSessionsQueryHandler(IAuthRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<IReadOnlyList<UserSessionResponse>>> Handle(GetActiveSessionsQuery query, CancellationToken cancellationToken)
    {
        var sessions = await _repo.GetActiveSessionsAsync(query.UserId, cancellationToken);

        var response = sessions
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new UserSessionResponse
            {
                Id = x.Id,
                CreatedAt = x.CreatedAt,
                IsCurrent = query.CurrentSessionId.HasValue && x.Id == query.CurrentSessionId.Value
            })
            .ToList();

        return Result<IReadOnlyList<UserSessionResponse>>.Success(response);
    }
}