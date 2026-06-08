using IdentityHub.Application.Common.Results;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;

namespace IdentityHub.Application.Interfaces;

public interface IAuditLogService
{
    Task<Result<PagedResponse<AuditLogItemResponse>>> GetPagedAsync(
        AuditLogFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<string> ExportCsvAsync(AuditLogFilter request, CancellationToken cancellationToken);
}
