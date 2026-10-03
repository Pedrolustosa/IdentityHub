using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.Sessions.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.Sessions.Handlers;

public sealed class GetSystemSessionsQueryHandler
    : IRequestHandler<GetSystemSessionsQuery, Result<PagedResponse<AdminSessionResponse>>>
{
    private readonly IAuthRepository _repository;

    public GetSystemSessionsQueryHandler(IAuthRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResponse<AdminSessionResponse>>> Handle(
        GetSystemSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, query.Page);
        var safePageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _repository.GetPagedSessionsAsync(
            query.Filter,
            safePage,
            safePageSize,
            cancellationToken);

        var response = new PagedResponse<AdminSessionResponse>
        {
            Items = items.Select(x => new AdminSessionResponse
            {
                Id = x.Session.Id,
                UserId = x.Session.UserId,
                Email = x.Email,
                FullName = x.FullName,
                IpAddress = x.Session.IpAddress,
                Browser = x.Session.Browser,
                OperatingSystem = x.Session.OperatingSystem,
                CreatedAt = x.Session.CreatedAt,
                LastAccessAt = x.Session.LastAccessAt,
                RevokedAt = x.Session.RevokedAt,
                IsActive = x.Session.IsActive,
                IsCurrent = query.CurrentSessionId.HasValue && x.Session.Id == query.CurrentSessionId.Value
            }).ToList(),
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize)
        };

        return Result<PagedResponse<AdminSessionResponse>>.Success(response);
    }
}
