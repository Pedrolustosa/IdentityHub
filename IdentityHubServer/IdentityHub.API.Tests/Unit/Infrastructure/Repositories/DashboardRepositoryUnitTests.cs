using IdentityHub.Domain.Constants;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using IdentityHub.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class DashboardRepositoryUnitTests
{
    [Fact]
    public async Task DashboardRepository_ShouldReturnExpectedCountsAndTrends()
    {
        await using var scope = await SqliteDbScope.CreateAsync();

        var today = DateTime.UtcNow;
        var tenDaysAgo = DateTime.UtcNow.AddDays(-10);
        var eightDaysAgo = DateTime.UtcNow.AddDays(-8);
        var threeDaysAgo = DateTime.UtcNow.AddDays(-3);

        scope.DbContext.Users.AddRange(
            new ApplicationUser { Id = "u1", Email = "u1@identityhub.com", UserName = "u1@identityhub.com", IsActive = true, IsDeleted = false, EmailConfirmed = false, CreatedAt = today },
            new ApplicationUser { Id = "u2", Email = "u2@identityhub.com", UserName = "u2@identityhub.com", IsActive = true, IsDeleted = false, EmailConfirmed = true, CreatedAt = threeDaysAgo },
            new ApplicationUser { Id = "u3", Email = "u3@identityhub.com", UserName = "u3@identityhub.com", IsActive = false, IsDeleted = false, EmailConfirmed = true, CreatedAt = eightDaysAgo },
            new ApplicationUser { Id = "u4", Email = "u4@identityhub.com", UserName = "u4@identityhub.com", IsActive = true, IsDeleted = false, EmailConfirmed = true, CreatedAt = tenDaysAgo }
        );

        scope.DbContext.UserSessions.AddRange(
            new UserSession { Id = Guid.NewGuid(), UserId = "u1", IsActive = true, CreatedAt = today },
            new UserSession { Id = Guid.NewGuid(), UserId = "u2", IsActive = false, CreatedAt = eightDaysAgo }
        );

        scope.DbContext.SecurityEvents.AddRange(
            new SecurityEvent { Id = Guid.NewGuid(), UserId = "u1", Type = SecurityAlertEventTypes.SuspiciousLogin, Status = SecurityEventStatus.Open, CreatedAt = today },
            new SecurityEvent { Id = Guid.NewGuid(), UserId = "u2", Type = SecurityAlertEventTypes.CriticalAction, Status = SecurityEventStatus.Resolved, CreatedAt = eightDaysAgo }
        );

        scope.DbContext.AuditLogEntries.AddRange(
            new AuditLogEntry { Id = Guid.NewGuid(), ActorUserId = "u1", Type = "Audit.User.Created", Description = "x", CreatedAt = today },
            new AuditLogEntry { Id = Guid.NewGuid(), ActorUserId = "u2", Type = "Audit.User.Updated", Description = "y", CreatedAt = eightDaysAgo }
        );

        await scope.DbContext.SaveChangesAsync();

        var repository = new DashboardRepository(scope.DbContext);

        Assert.Equal(4, await repository.GetTotalUsersAsync());
        Assert.Equal(3, await repository.GetActiveUsersAsync());
        Assert.Equal(1, await repository.GetActiveSessionsAsync());
        Assert.Equal(1, await repository.GetLoginsTodayAsync());
        Assert.Equal(2, await repository.GetNewUsersThisWeekAsync());
        Assert.Equal(1, await repository.GetSecurityEventsThisWeekAsync());
        Assert.Equal(1, await repository.GetOpenSecurityAlertsAsync());
        Assert.Equal(1, await repository.GetAuditedActionsTodayAsync());
        Assert.Equal(1, await repository.GetPendingInvitesAsync());
        Assert.Equal(0, await repository.GetTotalRolesAsync());
        Assert.Equal(1, await repository.GetSuspiciousSessionsAsync());

        Assert.Equal(2, await repository.GetNewUsersLastWeekAsync());
        Assert.Equal(1, await repository.GetSessionsLastWeekAsync());
        Assert.Equal(1, await repository.GetSecurityEventsLastWeekAsync());

        var loginTrend = await repository.GetLoginTrendAsync(3);
        var userTrend = await repository.GetNewUsersTrendAsync(3);

        Assert.Equal(3, loginTrend.Count);
        Assert.Equal(3, userTrend.Count);
        Assert.Contains(loginTrend, x => x.Value >= 0);
    }

    private sealed class SqliteDbScope : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext DbContext { get; }

        private SqliteDbScope(SqliteConnection connection, AppDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public static async Task<SqliteDbScope> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new SqliteDbScope(connection, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
