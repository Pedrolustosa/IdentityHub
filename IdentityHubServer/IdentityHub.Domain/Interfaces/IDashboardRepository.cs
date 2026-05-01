namespace IdentityHub.Domain.Interfaces;

public interface IDashboardRepository
{
    Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<int> GetNewUsersThisWeekAsync(CancellationToken cancellationToken = default);
    Task<int> GetSecurityEventsThisWeekAsync(CancellationToken cancellationToken = default);

    Task<int> GetUsersLastWeekAsync(CancellationToken cancellationToken = default);
    Task<int> GetSessionsLastWeekAsync(CancellationToken cancellationToken = default);
    Task<int> GetSecurityEventsLastWeekAsync(CancellationToken cancellationToken = default);
}