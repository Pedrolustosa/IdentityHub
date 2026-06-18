using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Constants;
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
                Severity = x.Severity,
                Status = x.Status,
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

    public async Task<Result<SecurityAlertItemResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var securityEvent = await _repository.GetByIdAsync(id, cancellationToken);

        if (securityEvent is null)
            return Result<SecurityAlertItemResponse>.Failure(
                Error.Create("SecurityAlert.NotFound", "Security alert not found"));

        return Result<SecurityAlertItemResponse>.Success(new SecurityAlertItemResponse
        {
            Id = securityEvent.Id,
            UserId = securityEvent.UserId,
            Type = securityEvent.Type,
            Severity = securityEvent.Severity,
            Status = securityEvent.Status,
            Description = securityEvent.Description,
            CreatedAt = securityEvent.CreatedAt
        });
    }

    public async Task<Result> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        var normalizedStatus = (status ?? string.Empty).Trim();

        var isValidStatus = SecurityEventStatus.All()
            .Any(allowed => string.Equals(allowed, normalizedStatus, StringComparison.OrdinalIgnoreCase));

        if (!isValidStatus)
            return Result.Failure(
                Error.Create(
                    "SecurityAlert.InvalidStatus",
                    $"Invalid status. Allowed values: {string.Join(", ", SecurityEventStatus.All())}"));

        var securityEvent = await _repository.GetByIdAsync(id, cancellationToken);

        if (securityEvent is null)
            return Result.Failure(Error.Create("SecurityAlert.NotFound", "Security alert not found"));

        securityEvent.Status = SecurityEventStatus.All()
            .First(allowed => string.Equals(allowed, normalizedStatus, StringComparison.OrdinalIgnoreCase));

        await _repository.UpdateAsync(securityEvent, cancellationToken);

        return Result.Success();
    }
}