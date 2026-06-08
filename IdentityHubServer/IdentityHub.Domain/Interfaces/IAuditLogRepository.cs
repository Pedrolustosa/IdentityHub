using IdentityHub.Domain.Entities;

namespace IdentityHub.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetPagedAsync(
        AuditLogFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task WriteAsync(string eventType, string description, CancellationToken cancellationToken = default);
}
