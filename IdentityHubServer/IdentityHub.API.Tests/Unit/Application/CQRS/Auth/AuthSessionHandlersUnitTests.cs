using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.CQRS.Auth.Handlers;
using IdentityHub.Application.CQRS.Auth.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthSessionHandlersUnitTests
{
    [Fact]
    public async Task GetActiveSessionsQueryHandler_ShouldSortAndMarkCurrentSession()
    {
        var repository = new FakeAuthRepository();
        var older = new UserSession { Id = Guid.NewGuid(), UserId = "u1", CreatedAt = DateTime.UtcNow.AddHours(-2), IsActive = true };
        var newer = new UserSession { Id = Guid.NewGuid(), UserId = "u1", CreatedAt = DateTime.UtcNow.AddHours(-1), IsActive = true };
        repository.ActiveSessionsByUserId["u1"] = [older, newer];

        var handler = new GetActiveSessionsQueryHandler(repository);

        var result = await handler.Handle(new GetActiveSessionsQuery("u1", older.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(newer.Id, result.Value![0].Id);
        Assert.Equal(older.Id, result.Value![1].Id);
        Assert.False(result.Value[0].IsCurrent);
        Assert.True(result.Value[1].IsCurrent);
    }

    [Fact]
    public async Task GetRecentSessionsQueryHandler_ShouldMapCurrentSessionFlag()
    {
        var repository = new FakeAuthRepository();
        var s1 = new UserSession { Id = Guid.NewGuid(), UserId = "u1", CreatedAt = DateTime.UtcNow, IsActive = true };
        var s2 = new UserSession { Id = Guid.NewGuid(), UserId = "u1", CreatedAt = DateTime.UtcNow.AddMinutes(-1), IsActive = false };
        repository.RecentSessionsByUserId["u1"] = [s1, s2];

        var handler = new GetRecentSessionsQueryHandler(repository);

        var result = await handler.Handle(new GetRecentSessionsQuery("u1", s2.Id, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.False(result.Value[0].IsCurrent);
        Assert.True(result.Value[1].IsCurrent);
    }

    [Fact]
    public async Task RevokeSessionCommandHandler_ShouldFail_WhenSessionNotFound()
    {
        var repository = new FakeAuthRepository();
        var audit = new FakeAuditLogRepository();
        var handler = new RevokeSessionCommandHandler(repository, audit);

        var result = await handler.Handle(new RevokeSessionCommand("u1", Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Session.NotFound", result.Error?.Code);
        Assert.Equal(0, repository.RevokeSessionCalls);
    }

    [Fact]
    public async Task RevokeSessionCommandHandler_ShouldFail_WhenSessionBelongsToAnotherUser()
    {
        var repository = new FakeAuthRepository();
        var audit = new FakeAuditLogRepository();
        var sessionId = Guid.NewGuid();
        repository.SessionsById[sessionId] = new UserSession
        {
            Id = sessionId,
            UserId = "other",
            IsActive = true
        };

        var handler = new RevokeSessionCommandHandler(repository, audit);

        var result = await handler.Handle(new RevokeSessionCommand("u1", sessionId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.Forbidden", result.Error?.Code);
        Assert.Equal(0, repository.RevokeSessionCalls);
        Assert.Equal(0, audit.WriteCalls);
    }

    [Fact]
    public async Task RevokeSessionCommandHandler_ShouldRevokeSessionAndTokensAndAudit_WhenValid()
    {
        var repository = new FakeAuthRepository();
        var audit = new FakeAuditLogRepository();
        var sessionId = Guid.NewGuid();
        repository.SessionsById[sessionId] = new UserSession
        {
            Id = sessionId,
            UserId = "u1",
            IsActive = true
        };
        repository.RefreshTokensBySessionId[sessionId] =
        [
            new RefreshToken { Id = Guid.NewGuid(), SessionId = sessionId, UserId = "u1" },
            new RefreshToken { Id = Guid.NewGuid(), SessionId = sessionId, UserId = "u1" }
        ];

        var handler = new RevokeSessionCommandHandler(repository, audit);

        var result = await handler.Handle(new RevokeSessionCommand("u1", sessionId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.RevokeSessionCalls);
        Assert.Equal(2, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal("Audit.Session.Revoked", audit.LastEventType);
    }

    [Fact]
    public async Task LogoutCommandHandler_ShouldFail_WhenRefreshTokenBelongsToAnotherUser()
    {
        var repository = new FakeAuthRepository();
        var tokenService = CreateTokenService();
        var refreshToken = "logout-mismatch-token";
        var tokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var sessionId = Guid.NewGuid();

        repository.RefreshTokensByHash[tokenHash] = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = tokenHash,
            UserId = "other-user",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        var handler = new LogoutCommandHandler(repository, tokenService);

        var result = await handler.Handle(
            new LogoutCommand("u1", new RefreshTokenRequest { RefreshToken = refreshToken }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.Forbidden", result.Error?.Code);
        Assert.Equal(0, repository.RevokeRefreshTokenCalls);
        Assert.Equal(0, repository.RevokeSessionCalls);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task LogoutCommandHandler_ShouldRevokeTokenAndSessionAndSave_WhenValid()
    {
        var repository = new FakeAuthRepository();
        var tokenService = CreateTokenService();
        var refreshToken = "logout-valid-token";
        var tokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var sessionId = Guid.NewGuid();
        repository.RefreshTokensByHash[tokenHash] = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = tokenHash,
            UserId = "u1",
            Expires = DateTime.UtcNow.AddHours(1)
        };
        repository.SessionsById[sessionId] = new UserSession
        {
            Id = sessionId,
            UserId = "u1",
            IsActive = true
        };

        var handler = new LogoutCommandHandler(repository, tokenService);

        var result = await handler.Handle(
            new LogoutCommand("u1", new RefreshTokenRequest { RefreshToken = refreshToken }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.RevokeSessionCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task RefreshCommandHandler_ShouldFail_WhenRefreshTokenNotFound()
    {
        var repository = new FakeAuthRepository();
        var tokenService = CreateTokenService();
        var alerts = new FakeSecurityAlertService();
        var handler = new RefreshCommandHandler(
            repository,
            tokenService,
            userManager: null!,
            roleManager: null!,
            securityAlertService: alerts);

        var result = await handler.Handle(
            new RefreshCommand(new RefreshTokenRequest { RefreshToken = "missing" }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefresh", result.Error?.Code);
        Assert.Equal(0, repository.SaveChangesCalls);
        Assert.Equal(0, alerts.RefreshTokenReuseCalls);
    }

    [Fact]
    public async Task RefreshCommandHandler_ShouldRevokeSessionTokensAndNotify_WhenTokenReuseDetected()
    {
        var repository = new FakeAuthRepository();
        var tokenService = CreateTokenService();
        var alerts = new FakeSecurityAlertService();
        var refreshToken = "revoked-token";
        var tokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var sessionId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "u1@identityhub.com",
            IsActive = true
        };

        repository.RefreshTokensByHash[tokenHash] = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = tokenHash,
            UserId = user.Id,
            User = user,
            Expires = DateTime.UtcNow.AddHours(1),
            IsRevoked = true
        };
        repository.SessionsById[sessionId] = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            IsActive = true
        };
        repository.RefreshTokensBySessionId[sessionId] =
        [
            new RefreshToken { Id = Guid.NewGuid(), SessionId = sessionId, UserId = user.Id, IsRevoked = false },
            new RefreshToken { Id = Guid.NewGuid(), SessionId = sessionId, UserId = user.Id, IsRevoked = false }
        ];

        var handler = new RefreshCommandHandler(
            repository,
            tokenService,
            userManager: null!,
            roleManager: null!,
            securityAlertService: alerts);

        var result = await handler.Handle(
            new RefreshCommand(new RefreshTokenRequest { RefreshToken = refreshToken }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefresh", result.Error?.Code);
        Assert.Equal(1, repository.RevokeSessionCalls);
        Assert.Equal(2, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal(1, alerts.RefreshTokenReuseCalls);
    }

    [Fact]
    public async Task RefreshCommandHandler_ShouldRevokeTokenAndFail_WhenUserIsInactive()
    {
        var repository = new FakeAuthRepository();
        var tokenService = CreateTokenService();
        var alerts = new FakeSecurityAlertService();
        var refreshToken = "inactive-user-token";
        var tokenHash = tokenService.ComputeRefreshTokenHash(refreshToken);
        var sessionId = Guid.NewGuid();

        repository.RefreshTokensByHash[tokenHash] = new RefreshToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            TokenHash = tokenHash,
            UserId = "u1",
            User = new ApplicationUser { Id = "u1", IsActive = false, IsDeleted = false },
            Expires = DateTime.UtcNow.AddHours(1),
            IsRevoked = false
        };

        var handler = new RefreshCommandHandler(
            repository,
            tokenService,
            userManager: null!,
            roleManager: null!,
            securityAlertService: alerts);

        var result = await handler.Handle(
            new RefreshCommand(new RefreshTokenRequest { RefreshToken = refreshToken }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidRefresh", result.Error?.Code);
        Assert.True(repository.RefreshTokensByHash[tokenHash].IsRevoked);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal(0, alerts.RefreshTokenReuseCalls);
    }

    private static TokenService CreateTokenService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-tests-jwt-key-with-at-least-32-bytes",
                ["Jwt:Issuer"] = "IdentityHub",
                ["Jwt:Audience"] = "IdentityHubUsers",
                ["Jwt:ExpireMinutes"] = "60"
            })
            .Build();

        return new TokenService(configuration);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public Dictionary<string, List<UserSession>> ActiveSessionsByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<UserSession>> RecentSessionsByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<Guid, UserSession> SessionsById { get; } = [];
        public Dictionary<Guid, List<RefreshToken>> RefreshTokensBySessionId { get; } = [];
        public Dictionary<string, RefreshToken> RefreshTokensByHash { get; } = new(StringComparer.Ordinal);

        public int RevokeSessionCalls { get; private set; }
        public int RevokeRefreshTokenCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
            => Task.FromResult(RefreshTokensByHash.TryGetValue(tokenHash, out var token) ? token : null);

        public Task<List<RefreshToken>> GetActiveRefreshTokensAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RefreshToken>());

        public Task<List<RefreshToken>> GetActiveRefreshTokensBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(RefreshTokensBySessionId.TryGetValue(sessionId, out var tokens) ? tokens : []);

        public Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            RevokeRefreshTokenCalls++;
            token.IsRevoked = true;
            return Task.CompletedTask;
        }

        public Task AddSessionAsync(UserSession session, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<List<UserSession>> GetActiveSessionsAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveSessionsByUserId.TryGetValue(userId, out var sessions) ? sessions : []);

        public Task<List<UserSession>> GetRecentSessionsAsync(string userId, int take, CancellationToken cancellationToken = default)
            => Task.FromResult(RecentSessionsByUserId.TryGetValue(userId, out var sessions) ? sessions.Take(take).ToList() : []);

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

    private sealed class FakeSecurityAlertService : IdentityHub.Application.Interfaces.ISecurityAlertService
    {
        public int SuspiciousLoginCalls { get; private set; }
        public int CriticalActionCalls { get; private set; }
        public int RefreshTokenReuseCalls { get; private set; }

        public Task NotifySuspiciousLoginAsync(ApplicationUser user, string reason, CancellationToken cancellationToken = default)
        {
            SuspiciousLoginCalls++;
            return Task.CompletedTask;
        }

        public Task NotifyCriticalActionAsync(ApplicationUser user, string actionTitle, string details, CancellationToken cancellationToken = default)
        {
            CriticalActionCalls++;
            return Task.CompletedTask;
        }

        public Task NotifyRefreshTokenReuseAsync(ApplicationUser user, Guid sessionId, CancellationToken cancellationToken = default)
        {
            RefreshTokenReuseCalls++;
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
