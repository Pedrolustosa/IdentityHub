using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

public interface ISecurityAlertsService
{
    Task<Result<PagedResponse<SecurityAlertItemResponse>>> GetPagedAsync(
        SecurityAlertFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}