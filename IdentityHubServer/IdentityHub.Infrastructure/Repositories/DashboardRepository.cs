using IdentityHub.Domain.Interfaces;
using IdentityHub.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public Task<int> GetActiveSessionsAsync(CancellationToken cancellationToken = default)
    {
        return _context.UserSessions
            .CountAsync(x => x.IsActive, cancellationToken);
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
}