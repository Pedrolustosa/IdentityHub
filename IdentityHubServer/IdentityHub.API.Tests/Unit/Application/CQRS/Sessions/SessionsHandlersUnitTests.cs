using IdentityHub.Application.CQRS.Sessions.Commands;
using IdentityHub.Application.CQRS.Sessions.Handlers;
using IdentityHub.Application.CQRS.Sessions.Queries;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class SessionsHandlersUnitTests
{
    [Fact]
    public async Task GetSystemSessionsQueryHandler_ShouldMapPagedItemsAndCurrentFlag()
    {
        var currentId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var repository = new FakeAuthRepository
        {
            PagedItems =
            [
                new UserSessionListItem
                {
                    Session = new UserSession
                    {
                        Id = currentId,
                        UserId = "u1",
                        IpAddress = "10.0.0.1",
                        Browser = "Chrome",
                        OperatingSystem = "Windows",
                        CreatedAt = DateTime.UtcNow.AddHours(-1),
                        IsActive = true
                    },
                    Email = "admin@identityhub.com",
                    FullName = "Admin"
                },
                new UserSessionListItem
                {
                    Session = new UserSession
                    {
                        Id = otherId,
                        UserId = "u2",
                        IpAddress = "10.0.0.2",
                        Browser = "Firefox",
                        OperatingSystem = "Linux",
                        CreatedAt = DateTime.UtcNow.AddHours(-2),
                        IsActive = true
                    },
                    Email = "user@identityhub.com",
                    FullName = "User"
                }
            ],
            PagedTotalCount = 2
        };

        var handler = new GetSystemSessionsQueryHandler(repository);

        var result = await handler.Handle(
            new GetSystemSessionsQuery(new SessionFilter { ActiveOnly = true }, 1, 20, currentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.True(result.Value.Items[0].IsCurrent);
        Assert.False(result.Value.Items[1].IsCurrent);
        Assert.Equal("admin@identityhub.com", result.Value.Items[0].Email);
        Assert.Equal("Chrome", result.Value.Items[0].Browser);
    }

    [Fact]
    public async Task RevokeSystemSessionCommandHandler_ShouldFail_WhenSessionMissingOrInactive()
    {
        var repository = new FakeAuthRepository();
        var audit = new FakeAuditLogRepository();
        var handler = new RevokeSystemSessionCommandHandler(repository, audit);

        var result = await handler.Handle(new RevokeSystemSessionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Session.NotFound", result.Error?.Code);
        Assert.Equal(0, repository.RevokeSessionCalls);
        Assert.Equal(0, audit.WriteCalls);
    }

    [Fact]
    public async Task RevokeSystemSessionCommandHandler_ShouldRevokeSessionAndTokens()
    {
        var sessionId = Guid.NewGuid();
        var session = new UserSession
        {
            Id = sessionId,
            UserId = "u1",
            IsActive = true
        };
        var repository = new FakeAuthRepository
        {
            SessionsById = { [sessionId] = session },
            RefreshTokensBySessionId =
            {
                [sessionId] =
                [
                    new RefreshToken { Id = Guid.NewGuid(), SessionId = sessionId, IsRevoked = false },
                    new RefreshToken { Id = Guid.NewGuid(), SessionId = sessionId, IsRevoked = false }
                ]
            }
        };
        var audit = new FakeAuditLogRepository();
        var handler = new RevokeSystemSessionCommandHandler(repository, audit);

        var result = await handler.Handle(new RevokeSystemSessionCommand(sessionId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(session.IsActive);
        Assert.Equal(1, repository.RevokeSessionCalls);
        Assert.Equal(2, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal("Audit.Session.Revoked", audit.LastEventType);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public List<UserSessionListItem> PagedItems { get; set; } = [];
        public int PagedTotalCount { get; set; }
        public Dictionary<Guid, UserSession> SessionsById { get; } = [];
        public Dictionary<Guid, List<RefreshToken>> RefreshTokensBySessionId { get; } = [];

        public int RevokeSessionCalls { get; private set; }
        public int RevokeRefreshTokenCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
            => Task.FromResult<RefreshToken?>(null);

        public Task<List<RefreshToken>> GetActiveRefreshTokensAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RefreshToken>());

        public Task<List<RefreshToken>> GetActiveRefreshTokensBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(RefreshTokensBySessionId.TryGetValue(sessionId, out var tokens)
                ? tokens.Where(x => !x.IsRevoked).ToList()
                : []);

        public Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            RevokeRefreshTokenCalls++;
            token.IsRevoked = true;
            return Task.CompletedTask;
        }

        public Task AddSessionAsync(UserSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<UserSession>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<UserSession>());

        public Task<List<UserSession>> GetRecentSessionsAsync(string userId, int take, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<UserSession>());

        public Task<(IReadOnlyList<UserSessionListItem> Items, int TotalCount)> GetPagedSessionsAsync(
            SessionFilter filter,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<UserSessionListItem>, int)>((PagedItems, PagedTotalCount));

        public Task<UserSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(SessionsById.TryGetValue(sessionId, out var session) ? session : null);

        public Task RevokeSessionAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            RevokeSessionCalls++;
            session.IsActive = false;
            session.RevokedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task AddSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls++;
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
}
