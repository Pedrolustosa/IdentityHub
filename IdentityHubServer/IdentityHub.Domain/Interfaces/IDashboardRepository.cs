namespace IdentityHub.Domain.Interfaces;

public interface IDashboardRepository
{
    Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveSessionsAsync(CancellationToken cancellationToken = default);
    Task<int> GetLoginsTodayAsync(CancellationToken cancellationToken = default);
    Task<int> GetNewUsersThisWeekAsync(CancellationToken cancellationToken = default);
    Task<int> GetSecurityEventsThisWeekAsync(CancellationToken cancellationToken = default);
    Task<int> GetOpenSecurityAlertsAsync(CancellationToken cancellationToken = default);
    Task<int> GetAuditedActionsTodayAsync(CancellationToken cancellationToken = default);
    Task<int> GetPendingInvitesAsync(CancellationToken cancellationToken = default);
    Task<int> GetTotalRolesAsync(CancellationToken cancellationToken = default);
    Task<int> GetSuspiciousSessionsAsync(CancellationToken cancellationToken = default);

    Task<int> GetNewUsersLastWeekAsync(CancellationToken cancellationToken = default);
    Task<int> GetSessionsLastWeekAsync(CancellationToken cancellationToken = default);
    Task<int> GetSecurityEventsLastWeekAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardTrendPoint>> GetLoginTrendAsync(int days, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DashboardTrendPoint>> GetNewUsersTrendAsync(int days, CancellationToken cancellationToken = default);
}

public sealed record DashboardTrendPoint(string Date, int Value);