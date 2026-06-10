using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;

namespace IdentityHub.Application.Services;

public sealed class SecurityAlertsService : ISecurityAlertsService
{
    private readonly ISecurityAlertRepository _repository;

    public SecurityAlertsService(ISecurityAlertRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResponse<SecurityAlertItemResponse>>> GetPagedAsync(
        SecurityAlertFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _repository.GetPagedAsync(request, safePage, safePageSize, cancellationToken);

        var response = new PagedResponse<SecurityAlertItemResponse>
        {
            Items = items.Select(x => new SecurityAlertItemResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                Type = x.Type,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Page = safePage,
            PageSize = safePageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize)
        };

        return Result<PagedResponse<SecurityAlertItemResponse>>.Success(response);
    }
}