using IdentityHub.Application.CQRS.Dashboard.Handlers;
using IdentityHub.Application.CQRS.Dashboard.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Constants;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class DashboardQueryHandlerUnitTests
{
    [Fact]
    public async Task GetDashboardQueryHandler_ShouldMapDashboardAndComputeGrowthAndPermissionCards()
    {
        var dashboardRepo = new FakeDashboardRepository
        {
            TotalUsers = 10,
            ActiveUsers = 8,
            ActiveSessions = 6,
            LoginsToday = 5,
            NewUsersThisWeek = 4,
            SecurityEventsThisWeek = 3,
            OpenSecurityAlerts = 2,
            AuditedActionsToday = 7,
            PendingInvites = 1,
            TotalRoles = 2,
            SuspiciousSessions = 1,
            NewUsersLastWeek = 2,
            SessionsLastWeek = 3,
            SecurityEventsLastWeek = 1,
            LoginTrend = [new DashboardTrendPoint("2026-06-27", 10)],
            NewUsersTrend = [new DashboardTrendPoint("2026-06-27", 2)]
        };

        var auditRepo = new FakeAuditLogRepository();
        var alertsRepo = new FakeSecurityAlertRepository();
        var roleRepo = new FakeRoleRepository();

        roleRepo.Roles.Add(new IdentityRole("Admin") { Id = "r1" });
        roleRepo.Roles.Add(new IdentityRole("Viewer") { Id = "r2" });
        roleRepo.ClaimsByRoleId["r1"] = [new Claim("permission", AppPermissions.Users.View)];
        roleRepo.ClaimsByRoleId["r2"] = [new Claim("permission", AppPermissions.Roles.View)];

        var handler = new GetDashboardQueryHandler(
            dashboardRepo,
            auditRepo,
            alertsRepo,
            roleRepo,
            Options.Create(new SmtpSettings
            {
                Host = "smtp.local",
                Port = 587,
                Username = "user",
                From = "noreply@identityhub.com"
            }));

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var value = result.Value!;

        Assert.Equal(10, value.TotalUsers);
        Assert.Equal(200, value.SecurityGrowth);
        Assert.Equal(100, value.UsersGrowth);
        Assert.Equal(100, value.SessionsGrowth);
        Assert.True(value.SystemStatus.EmailConfigured);
        Assert.Single(value.LoginTrend);
        Assert.Single(value.NewUsersTrend);
        Assert.Single(value.RecentAuditActions);
        Assert.Single(value.RecentSecurityAlerts);

        var usersViewCard = value.PermissionCards.Single(x => x.Permission == AppPermissions.Users.View);
        Assert.Equal(1, usersViewCard.RolesGranted);
    }

    [Fact]
    public async Task GetDashboardQueryHandler_ShouldMarkEmailNotConfigured_WhenMissingSettings()
    {
        var handler = new GetDashboardQueryHandler(
            new FakeDashboardRepository(),
            new FakeAuditLogRepository(),
            new FakeSecurityAlertRepository(),
            new FakeRoleRepository(),
            Options.Create(new SmtpSettings()));

        var result = await handler.Handle(new GetDashboardQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.SystemStatus.EmailConfigured);
    }

    private sealed class FakeDashboardRepository : IDashboardRepository
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveSessions { get; set; }
        public int LoginsToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int SecurityEventsThisWeek { get; set; }
        public int OpenSecurityAlerts { get; set; }
        public int AuditedActionsToday { get; set; }
        public int PendingInvites { get; set; }
        public int TotalRoles { get; set; }
        public int SuspiciousSessions { get; set; }
        public int NewUsersLastWeek { get; set; }
        public int SessionsLastWeek { get; set; }
        public int SecurityEventsLastWeek { get; set; }
        public IReadOnlyList<DashboardTrendPoint> LoginTrend { get; set; } = [];
        public IReadOnlyList<DashboardTrendPoint> NewUsersTrend { get; set; } = [];

        public Task<int> GetTotalUsersAsync(CancellationToken cancellationToken = default) => Task.FromResult(TotalUsers);
        public Task<int> GetActiveUsersAsync(CancellationToken cancellationToken = default) => Task.FromResult(ActiveUsers);
        public Task<int> GetActiveSessionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(ActiveSessions);
        public Task<int> GetLoginsTodayAsync(CancellationToken cancellationToken = default) => Task.FromResult(LoginsToday);
        public Task<int> GetNewUsersThisWeekAsync(CancellationToken cancellationToken = default) => Task.FromResult(NewUsersThisWeek);
        public Task<int> GetSecurityEventsThisWeekAsync(CancellationToken cancellationToken = default) => Task.FromResult(SecurityEventsThisWeek);
        public Task<int> GetOpenSecurityAlertsAsync(CancellationToken cancellationToken = default) => Task.FromResult(OpenSecurityAlerts);
        public Task<int> GetAuditedActionsTodayAsync(CancellationToken cancellationToken = default) => Task.FromResult(AuditedActionsToday);
        public Task<int> GetPendingInvitesAsync(CancellationToken cancellationToken = default) => Task.FromResult(PendingInvites);
        public Task<int> GetTotalRolesAsync(CancellationToken cancellationToken = default) => Task.FromResult(TotalRoles);
        public Task<int> GetSuspiciousSessionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(SuspiciousSessions);
        public Task<int> GetNewUsersLastWeekAsync(CancellationToken cancellationToken = default) => Task.FromResult(NewUsersLastWeek);
        public Task<int> GetSessionsLastWeekAsync(CancellationToken cancellationToken = default) => Task.FromResult(SessionsLastWeek);
        public Task<int> GetSecurityEventsLastWeekAsync(CancellationToken cancellationToken = default) => Task.FromResult(SecurityEventsLastWeek);
        public Task<IReadOnlyList<DashboardTrendPoint>> GetLoginTrendAsync(int days, CancellationToken cancellationToken = default) => Task.FromResult(LoginTrend);
        public Task<IReadOnlyList<DashboardTrendPoint>> GetNewUsersTrendAsync(int days, CancellationToken cancellationToken = default) => Task.FromResult(NewUsersTrend);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetPagedAsync(AuditLogFilter request, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<AuditLogEntry>, int)>(([new AuditLogEntry { Id = Guid.NewGuid(), Type = "x", Description = "desc" }], 1));

        public Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<AuditLogEntry?>(null);

        public Task<IReadOnlyList<AuditLogEntry>> GetRecentByUserAsync(string userId, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AuditLogEntry>>([]);

        public Task WriteAsync(string eventType, string description, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task WriteAsync(string eventType, string description, string? targetId, object? metadata, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeSecurityAlertRepository : ISecurityAlertRepository
    {
        public Task<(IReadOnlyList<SecurityEvent> Items, int TotalCount)> GetPagedAsync(SecurityAlertFilter request, int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<SecurityEvent>, int)>(([new SecurityEvent { Id = Guid.NewGuid(), Type = "alert", Description = "desc" }], 1));

        public Task<SecurityEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult<SecurityEvent?>(null);

        public Task UpdateAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        public List<IdentityRole> Roles { get; } = [];
        public Dictionary<string, IList<Claim>> ClaimsByRoleId { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<List<IdentityRole>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.ToList());

        public Task<IdentityRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(x => x.Id == id));

        public Task<IdentityRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<int> GetUserCountAsync(string roleId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IDictionary<string, int>> GetUserCountsByRoleIdAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IDictionary<string, int>>(new Dictionary<string, int>());

        public Task CreateAsync(IdentityRole role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(IdentityRole role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(IdentityRole role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default)
            => Task.FromResult(ClaimsByRoleId.TryGetValue(role.Id, out var claims) ? claims : (IList<Claim>)[]);

        public Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
