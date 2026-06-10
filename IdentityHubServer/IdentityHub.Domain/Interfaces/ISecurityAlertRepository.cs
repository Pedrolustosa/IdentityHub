using IdentityHub.Domain.Entities;

namespace IdentityHub.Domain.Interfaces;

public interface ISecurityAlertRepository
{
    Task<(IReadOnlyList<SecurityEvent> Items, int TotalCount)> GetPagedAsync(
        SecurityAlertFilter request,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}