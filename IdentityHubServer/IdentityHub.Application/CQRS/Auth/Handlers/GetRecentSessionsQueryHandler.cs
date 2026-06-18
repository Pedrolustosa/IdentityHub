using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Auth.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Auth.Handlers;

public sealed class GetRecentSessionsQueryHandler : IRequestHandler<GetRecentSessionsQuery, Result<IReadOnlyList<UserSessionResponse>>>
{
    private readonly IAuthRepository _repo;

    public GetRecentSessionsQueryHandler(IAuthRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<IReadOnlyList<UserSessionResponse>>> Handle(
        GetRecentSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var sessions = await _repo.GetRecentSessionsAsync(query.UserId, query.Take, cancellationToken);

        var response = sessions
            .Select(x => new UserSessionResponse
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                Browser = x.Browser,
                OperatingSystem = x.OperatingSystem,
                CreatedAt = x.CreatedAt,
                LastAccessAt = x.LastAccessAt,
                RevokedAt = x.RevokedAt,
                IsActive = x.IsActive,
                IsCurrent = query.CurrentSessionId.HasValue && x.Id == query.CurrentSessionId.Value
            })
            .ToList();

        return Result<IReadOnlyList<UserSessionResponse>>.Success(response);
    }
}
