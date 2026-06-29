using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Application.CQRS.Users.Handlers;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UsersCommandHandlersUnitTests
{
    [Fact]
    public async Task CreateUserCommandHandler_ShouldFail_WhenEmailOrPasswordMissing()
    {
        var userRepo = new FakeUserRepository();
        var auditRepo = new FakeAuditLogRepository();
        var handler = new CreateUserCommandHandler(userRepo, auditRepo);

        var result = await handler.Handle(
            new CreateUserCommand(new CreateUserRequest
            {
                Email = " ",
                Password = ""
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.InvalidRequest", result.Error?.Code);
        Assert.Equal(0, userRepo.CreateCalls);
        Assert.Equal(0, auditRepo.WriteCalls);
    }

    [Fact]
    public async Task CreateUserCommandHandler_ShouldFail_WhenUserAlreadyExists()
    {
        var userRepo = new FakeUserRepository();
        var auditRepo = new FakeAuditLogRepository();
        userRepo.Users.Add(new ApplicationUser
        {
            Id = "u1",
            Email = "existing@identityhub.com",
            UserName = "existing@identityhub.com"
        });

        var handler = new CreateUserCommandHandler(userRepo, auditRepo);

        var result = await handler.Handle(
            new CreateUserCommand(new CreateUserRequest
            {
                Email = "existing@identityhub.com",
                Password = "Password@123",
                FullName = "Existing User"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.AlreadyExists", result.Error?.Code);
        Assert.Equal(0, userRepo.CreateCalls);
        Assert.Equal(0, auditRepo.WriteCalls);
    }

    [Fact]
    public async Task CreateUserCommandHandler_ShouldCreateNormalizedUser_AndWriteAudit()
    {
        var userRepo = new FakeUserRepository();
        var auditRepo = new FakeAuditLogRepository();
        var handler = new CreateUserCommandHandler(userRepo, auditRepo);

        var result = await handler.Handle(
            new CreateUserCommand(new CreateUserRequest
            {
                Email = "  New.User@IdentityHub.COM ",
                Password = "Password@123",
                FullName = "  New User  "
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, userRepo.CreateCalls);
        Assert.NotNull(userRepo.LastCreatedUser);
        Assert.Equal("new.user@identityhub.com", userRepo.LastCreatedUser!.Email);
        Assert.Equal("new.user@identityhub.com", userRepo.LastCreatedUser.UserName);
        Assert.Equal("New User", userRepo.LastCreatedUser.FullName);
        Assert.True(userRepo.LastCreatedUser.IsActive);
        Assert.True(userRepo.LastCreatedUser.EmailConfirmed);
        Assert.Equal(1, auditRepo.WriteCalls);
        Assert.Equal("Audit.User.Created", auditRepo.LastEventType);
    }

    [Fact]
    public async Task DeleteUserCommandHandler_ShouldFail_WhenUserNotFound()
    {
        var userRepo = new FakeUserRepository();
        var authRepo = new FakeAuthRepository();
        var currentUser = new FakeCurrentUserContext("admin-user");
        var auditRepo = new FakeAuditLogRepository();
        var alerts = new FakeSecurityAlertService();

        var handler = new DeleteUserCommandHandler(userRepo, authRepo, currentUser, auditRepo, alerts);

        var result = await handler.Handle(new DeleteUserCommand("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error?.Code);
        Assert.Equal(0, userRepo.DeleteCalls);
        Assert.Equal(0, authRepo.SaveChangesCalls);
        Assert.Equal(0, alerts.CriticalActionCalls);
    }

    [Fact]
    public async Task DeleteUserCommandHandler_ShouldDeleteInvalidateSessionsAndTokens_AuditAndNotify()
    {
        var userRepo = new FakeUserRepository();
        var targetUser = new ApplicationUser
        {
            Id = "u1",
            Email = "target@identityhub.com",
            FullName = "Target User",
            IsActive = true
        };
        userRepo.Users.Add(targetUser);

        var authRepo = new FakeAuthRepository();
        authRepo.ActiveSessionsByUserId["u1"] =
        [
            new UserSession { Id = Guid.NewGuid(), UserId = "u1", IsActive = true },
            new UserSession { Id = Guid.NewGuid(), UserId = "u1", IsActive = true }
        ];
        authRepo.ActiveRefreshTokensByUserId["u1"] =
        [
            new RefreshToken { Id = Guid.NewGuid(), UserId = "u1", IsRevoked = false },
            new RefreshToken { Id = Guid.NewGuid(), UserId = "u1", IsRevoked = false }
        ];

        var currentUser = new FakeCurrentUserContext("admin-user");
        var auditRepo = new FakeAuditLogRepository();
        var alerts = new FakeSecurityAlertService();

        var handler = new DeleteUserCommandHandler(userRepo, authRepo, currentUser, auditRepo, alerts);

        var result = await handler.Handle(new DeleteUserCommand("u1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, userRepo.DeleteCalls);
        Assert.Equal("admin-user", userRepo.LastDeletedBy);
        Assert.Equal(1, authRepo.SaveChangesCalls);
        Assert.All(authRepo.ActiveSessionsByUserId["u1"], session => Assert.False(session.IsActive));
        Assert.All(authRepo.ActiveRefreshTokensByUserId["u1"], token => Assert.True(token.IsRevoked));
        Assert.Equal(1, auditRepo.WriteCalls);
        Assert.Equal("Audit.User.Deleted", auditRepo.LastEventType);
        Assert.Equal(1, alerts.CriticalActionCalls);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<ApplicationUser> Users { get; } = [];

        public int CreateCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public ApplicationUser? LastCreatedUser { get; private set; }
        public string? LastDeletedBy { get; private set; }

        public Task<List<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Users.ToList());

        public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(x => x.Id == id));

        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
            => Task.FromResult<IList<string>>([]);

        public Task<DateTime?> GetLastLoginAtAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult<DateTime?>(null);

        public Task<int> GetActiveSessionsCountAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task CreateAsync(ApplicationUser user, string password, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastCreatedUser = user;
            if (string.IsNullOrWhiteSpace(user.Id))
                user.Id = Guid.NewGuid().ToString();
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(ApplicationUser user, string? deletedBy, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            LastDeletedBy = deletedBy;
            user.IsDeleted = true;
            user.IsActive = false;
            user.DeletedBy = deletedBy;
            user.DeletedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task UpdateRolesAsync(ApplicationUser user, IList<string> roles, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public Dictionary<string, List<UserSession>> ActiveSessionsByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<RefreshToken>> ActiveRefreshTokensByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int SaveChangesCalls { get; private set; }

        public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
            => Task.FromResult<RefreshToken?>(null);

        public Task<List<RefreshToken>> GetActiveRefreshTokensAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveRefreshTokensByUserId.TryGetValue(userId, out var tokens) ? tokens : []);

        public Task<List<RefreshToken>> GetActiveRefreshTokensBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RefreshToken>());

        public Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddSessionAsync(UserSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<UserSession>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveSessionsByUserId.TryGetValue(userId, out var sessions) ? sessions : []);

        public Task<List<UserSession>> GetRecentSessionsAsync(string userId, int take, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<UserSession>());

        public Task<UserSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult<UserSession?>(null);

        public Task RevokeSessionAsync(UserSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public FakeCurrentUserContext(string? userId)
        {
            UserId = userId;
        }

        public string? UserId { get; }
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
