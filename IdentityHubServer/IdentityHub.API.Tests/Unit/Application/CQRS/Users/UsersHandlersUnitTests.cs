using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Application.CQRS.Users.Handlers;
using IdentityHub.Application.CQRS.Users.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UsersHandlersUnitTests
{
    [Fact]
    public async Task GetUserByIdQueryHandler_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        var repository = new FakeUserRepository();
        var handler = new GetUserByIdQueryHandler(repository);

        var result = await handler.Handle(new GetUserByIdQuery("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task GetUsersQueryHandler_ShouldMapUserDataAndEnrichWithSessionsAndRoles()
    {
        var repository = new FakeUserRepository();
        repository.Users.Add(new ApplicationUser
        {
            Id = "u1",
            Email = "a@identityhub.com",
            FullName = "Alice",
            IsActive = true,
            EmailConfirmed = true
        });
        repository.Users.Add(new ApplicationUser
        {
            Id = "u2",
            Email = "b@identityhub.com",
            FullName = "Bob",
            IsActive = false,
            EmailConfirmed = false
        });
        repository.RolesByUserId["u1"] = ["Admin"];
        repository.RolesByUserId["u2"] = ["Manager", "User"];
        repository.ActiveSessionsByUserId["u1"] = 3;
        repository.ActiveSessionsByUserId["u2"] = 0;

        var handler = new GetUsersQueryHandler(repository);

        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(3, result.Value.Single(x => x.Id == "u1").ActiveSessions);
        Assert.Equal(new[] { "Manager", "User" }, result.Value.Single(x => x.Id == "u2").Roles);
    }

    [Fact]
    public async Task UpdateUserCommandHandler_ShouldTrimNameUpdateAndWriteAudit()
    {
        var repository = new FakeUserRepository();
        var audit = new FakeAuditLogRepository();
        repository.Users.Add(new ApplicationUser
        {
            Id = "u1",
            Email = "user@identityhub.com",
            FullName = "Old Name",
            IsActive = true
        });

        var handler = new UpdateUserCommandHandler(repository, audit);

        var result = await handler.Handle(
            new UpdateUserCommand("u1", new UpdateUserRequest { FullName = "  New Name  ", IsActive = false }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.UpdateCalls);
        Assert.Equal("New Name", repository.Users.Single(x => x.Id == "u1").FullName);
        Assert.False(repository.Users.Single(x => x.Id == "u1").IsActive);
        Assert.Equal(1, audit.WriteCalls);
        Assert.Equal("Audit.User.Updated", audit.LastEventType);
    }

    [Fact]
    public async Task UpdateUserRolesCommandHandler_ShouldFail_WhenRolesAreEmpty()
    {
        var repository = new FakeUserRepository();
        var audit = new FakeAuditLogRepository();
        var alerts = new FakeSecurityAlertService();
        repository.Users.Add(new ApplicationUser { Id = "u1", Email = "user@identityhub.com" });

        var handler = new UpdateUserRolesCommandHandler(repository, audit, alerts);

        var result = await handler.Handle(
            new UpdateUserRolesCommand("u1", new UpdateRolesRequest { Roles = [] }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.RolesRequired", result.Error?.Code);
        Assert.Equal(0, repository.UpdateRolesCalls);
        Assert.Equal(0, alerts.CriticalActionCalls);
        Assert.Equal(0, audit.WriteCalls);
    }

    [Fact]
    public async Task UpdateUserRolesCommandHandler_ShouldUpdateRolesAndNotifyAndAudit()
    {
        var repository = new FakeUserRepository();
        var audit = new FakeAuditLogRepository();
        var alerts = new FakeSecurityAlertService();
        repository.Users.Add(new ApplicationUser { Id = "u1", Email = "user@identityhub.com" });

        var handler = new UpdateUserRolesCommandHandler(repository, audit, alerts);

        var result = await handler.Handle(
            new UpdateUserRolesCommand("u1", new UpdateRolesRequest { Roles = [" Admin ", "User"] }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.UpdateRolesCalls);
        Assert.Equal(new[] { " Admin ", "User" }, repository.LastUpdatedRoles);
        Assert.Equal(1, alerts.CriticalActionCalls);
        Assert.Equal(1, audit.WriteCalls);
        Assert.Equal("Audit.User.RolesUpdated", audit.LastEventType);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<ApplicationUser> Users { get; } = [];
        public Dictionary<string, IList<string>> RolesByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, DateTime?> LastLoginByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> ActiveSessionsByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int UpdateCalls { get; private set; }
        public int UpdateRolesCalls { get; private set; }
        public IList<string>? LastUpdatedRoles { get; private set; }

        public Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Users.ToList());

        public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(x => x.Id == id));

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
            => Task.FromResult(RolesByUserId.TryGetValue(user.Id, out var roles) ? roles : (IList<string>)[]);

        public Task<DateTime?> GetLastLoginAtAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(LastLoginByUserId.TryGetValue(userId, out var dt) ? dt : null);

        public Task<int> GetActiveSessionsCountAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveSessionsByUserId.TryGetValue(userId, out var count) ? count : 0);

        public Task CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ApplicationUser user, string? deletedBy, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateRolesAsync(ApplicationUser user, IList<string> roles, CancellationToken cancellationToken = default)
        {
            UpdateRolesCalls++;
            LastUpdatedRoles = roles;
            RolesByUserId[user.Id] = roles;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public int WriteCalls { get; private set; }
        public string LastEventType { get; private set; } = string.Empty;

        public Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetPagedAsync(AuditLogFilter request, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<AuditLogEntry>> GetRecentByUserAsync(string userId, int take, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task WriteAsync(string eventType, string description, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            LastEventType = eventType;
            return Task.CompletedTask;
        }

        public Task WriteAsync(string eventType, string description, string? targetId, object? metadata, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            LastEventType = eventType;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSecurityAlertService : ISecurityAlertService
    {
        public int CriticalActionCalls { get; private set; }

        public Task NotifySuspiciousLoginAsync(ApplicationUser user, string reason, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task NotifyCriticalActionAsync(ApplicationUser user, string actionTitle, string details, CancellationToken cancellationToken = default)
        {
            CriticalActionCalls++;
            return Task.CompletedTask;
        }

        public Task NotifyRefreshTokenReuseAsync(ApplicationUser user, Guid sessionId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
