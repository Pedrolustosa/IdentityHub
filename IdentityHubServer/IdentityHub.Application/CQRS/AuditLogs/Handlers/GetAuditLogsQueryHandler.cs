using IdentityHub.Application.Common.Results;
using IdentityHub.Application.CQRS.AuditLogs.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Interfaces;
using MediatR;

namespace IdentityHub.Application.CQRS.AuditLogs.Handlers;

public sealed class GetAuditLogsQueryHandler
    : IRequestHandler<GetAuditLogsQuery, Result<PagedResponse<AuditLogItemResponse>>>
{
    private readonly IAuditLogRepository _repository;

    public GetAuditLogsQueryHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResponse<AuditLogItemResponse>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, totalCount) = await _repository.GetPagedAsync(request.Request, page, pageSize, cancellationToken);

        var response = new PagedResponse<AuditLogItemResponse>
        {
            Items = items.Select(x => new AuditLogItemResponse
            {
                Id = x.Id,
                ActorUserId = x.ActorUserId,
                Type = x.Type,
                TargetId = x.TargetId,
                Description = x.Description,
                MetadataJson = x.MetadataJson,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return Result<PagedResponse<AuditLogItemResponse>>.Success(response);
    }
}
