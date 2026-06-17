using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using IdentityHub.Domain.Constants;
using System.Globalization;

namespace IdentityHub.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _context;

    public DashboardRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default)
    {
        return _context.Users.CountAsync(cancellationToken);
    }

    public Task<int> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return _context.Users.CountAsync(user => user.IsActive && !user.IsDeleted, cancellationToken);
    }

    public Task<int> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return _context.UserSessions
            .CountAsync(x => x.IsActive, cancellationToken);
    }

    public Task<int> GetLoginsTodayAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.Date;

        return _context.UserSessions
            .CountAsync(session => session.CreatedAt >= start, cancellationToken);
    }

    public Task<int> GetNewUsersThisWeekAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.AddDays(-7);

        return _context.Users
            .CountAsync(x => x.CreatedAt >= start, cancellationToken);
    }

    public Task<int> GetSecurityEventsThisWeekAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.AddDays(-7);

        return _context.SecurityEvents
            .CountAsync(x => x.CreatedAt >= start, cancellationToken);
    }

    public Task<int> GetOpenSecurityAlertsAsync(CancellationToken cancellationToken = default)
    {
        return _context.SecurityEvents.CountAsync(eventItem => eventItem.Status == SecurityEventStatus.Open, cancellationToken);
    }

    public Task<int> GetAuditedActionsTodayAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.Date;

        return _context.AuditLogEntries
            .CountAsync(entry => entry.CreatedAt >= start, cancellationToken);
    }

    public Task<int> GetPendingInvitesAsync(CancellationToken cancellationToken = default)
    {
        return _context.Users.CountAsync(user => !user.EmailConfirmed && user.IsActive && !user.IsDeleted, cancellationToken);
    }

    public Task<int> GetTotalRolesAsync(CancellationToken cancellationToken = default)
    {
        return _context.Roles.CountAsync(cancellationToken);
    }

    public Task<int> GetSuspiciousSessionsAsync(CancellationToken cancellationToken = default)
    {
        return _context.SecurityEvents.CountAsync(
            eventItem =>
                eventItem.Type == SecurityAlertEventTypes.SuspiciousLogin &&
                eventItem.Status == SecurityEventStatus.Open,
            cancellationToken);
    }

    public Task<int> GetNewUsersLastWeekAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.AddDays(-14);
        var end = DateTime.UtcNow.AddDays(-7);

        return _context.Users
            .CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end, cancellationToken);
    }

    public Task<int> GetSessionsLastWeekAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.AddDays(-14);
        var end = DateTime.UtcNow.AddDays(-7);

        return _context.UserSessions
            .CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end, cancellationToken);
    }

    public Task<int> GetSecurityEventsLastWeekAsync(CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.AddDays(-14);
        var end = DateTime.UtcNow.AddDays(-7);

        return _context.SecurityEvents
            .CountAsync(x => x.CreatedAt >= start && x.CreatedAt < end, cancellationToken);
    }

    public Task<IReadOnlyList<DashboardTrendPoint>> GetLoginTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        return BuildTrendAsync(
            _context.UserSessions.AsNoTracking().Select(session => session.CreatedAt),
            days,
            cancellationToken);
    }

    public Task<IReadOnlyList<DashboardTrendPoint>> GetNewUsersTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        return BuildTrendAsync(
            _context.Users.AsNoTracking().Select(user => user.CreatedAt),
            days,
            cancellationToken);
    }

    private static async Task<IReadOnlyList<DashboardTrendPoint>> BuildTrendAsync(
        IQueryable<DateTime> source,
        int days,
        CancellationToken cancellationToken)
    {
        var safeDays = Math.Max(1, days);
        var start = DateTime.UtcNow.Date.AddDays(-(safeDays - 1));

        var values = await source
            .Where(date => date >= start)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, safeDays)
            .Select(offset => start.AddDays(offset))
            .Select(day => new DashboardTrendPoint(
                day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                values.Count(value => value.Date == day)))
            .ToList();
    }
}