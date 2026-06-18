using IdentityHub.Domain.Entities;

namespace IdentityHub.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetPagedAsync(
        AuditLogFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogEntry>> GetRecentByUserAsync(
        string userId,
        int take,
        CancellationToken cancellationToken = default);

    Task WriteAsync(string eventType, string description, CancellationToken cancellationToken = default);

    Task WriteAsync(
        string eventType,
        string description,
        string? targetId,
        object? metadata,
        CancellationToken cancellationToken = default);
}
