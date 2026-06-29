using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.CQRS.Auth.Handlers;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthLoginAndPasswordHandlersUnitTests
{
    [Fact]
    public async Task LoginCommandHandler_ShouldFail_WhenUserIsMissingOrInactive()
    {
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(null)
        };

        var handler = new LoginCommandHandler(
            userManager,
            roleManager: null!,
            tokenService: CreateTokenService(),
            authRepository: new FakeAuthRepository(),
            clientDeviceInfoProvider: new FakeDeviceInfoProvider(),
            securityAlertService: new FakeSecurityAlertService());

        var result = await handler.Handle(
            new LoginCommand(new LoginRequest { Email = "missing@identityhub.com", Password = "Password@123" }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error?.Code);
    }

    [Fact]
    public async Task LoginCommandHandler_ShouldFailAndNotifySuspiciousLogin_WhenPasswordIsInvalid()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "u1@identityhub.com",
            IsActive = true,
            IsDeleted = false,
            EmailConfirmed = true
        };

        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnCheckPasswordAsync = (_, _) => Task.FromResult(false)
        };

        var alerts = new FakeSecurityAlertService();

        var handler = new LoginCommandHandler(
            userManager,
            roleManager: null!,
            tokenService: CreateTokenService(),
            authRepository: new FakeAuthRepository(),
            clientDeviceInfoProvider: new FakeDeviceInfoProvider(),
            securityAlertService: alerts);

        var result = await handler.Handle(
            new LoginCommand(new LoginRequest { Email = "u1@identityhub.com", Password = "wrong" }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.InvalidCredentials", result.Error?.Code);
        Assert.Equal(1, alerts.SuspiciousLoginCalls);
    }

    [Fact]
    public async Task LoginCommandHandler_ShouldFail_WhenEmailNotConfirmed()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "u1@identityhub.com",
            IsActive = true,
            IsDeleted = false,
            EmailConfirmed = false
        };

        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnCheckPasswordAsync = (_, _) => Task.FromResult(true)
        };

        var repository = new FakeAuthRepository();

        var handler = new LoginCommandHandler(
            userManager,
            roleManager: null!,
            tokenService: CreateTokenService(),
            authRepository: repository,
            clientDeviceInfoProvider: new FakeDeviceInfoProvider(),
            securityAlertService: new FakeSecurityAlertService());

        var result = await handler.Handle(
            new LoginCommand(new LoginRequest { Email = "u1@identityhub.com", Password = "Password@123" }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.EmailNotConfirmed", result.Error?.Code);
        Assert.Equal(0, repository.AddRefreshTokenCalls);
        Assert.Equal(0, repository.AddSessionCalls);
    }

    [Fact]
    public async Task LoginCommandHandler_ShouldPersistSessionAndRefreshToken_WhenValid()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "u1@identityhub.com",
            UserName = "u1@identityhub.com",
            FullName = "User One",
            IsActive = true,
            IsDeleted = false,
            EmailConfirmed = true
        };

        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnCheckPasswordAsync = (_, _) => Task.FromResult(true),
            OnGetRolesAsync = _ => Task.FromResult<IList<string>>([]),
            OnGetClaimsAsync = _ => Task.FromResult<IList<System.Security.Claims.Claim>>([])
        };

        var repository = new FakeAuthRepository();
        var handler = new LoginCommandHandler(
            userManager,
            roleManager: null!,
            tokenService: CreateTokenService(),
            authRepository: repository,
            clientDeviceInfoProvider: new FakeDeviceInfoProvider("10.1.1.1", "Chrome", "Windows"),
            securityAlertService: new FakeSecurityAlertService());

        var result = await handler.Handle(
            new LoginCommand(new LoginRequest { Email = " u1@identityhub.com ", Password = "Password@123" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value!.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.Equal(1, repository.AddRefreshTokenCalls);
        Assert.Equal(1, repository.AddSessionCalls);
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal("10.1.1.1", repository.LastAddedSession?.IpAddress);
        Assert.Equal("Chrome", repository.LastAddedSession?.Browser);
        Assert.Equal("Windows", repository.LastAddedSession?.OperatingSystem);
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_ShouldFail_WhenUserNotFound()
    {
        var userManager = new StubUserManager
        {
            OnFindByIdAsync = _ => Task.FromResult<ApplicationUser?>(null)
        };

        var handler = new ChangePasswordCommandHandler(
            userManager,
            repo: new FakeAuthRepository(),
            securityAlertService: new FakeSecurityAlertService(),
            auditLogRepository: new FakeAuditLogRepository());

        var result = await handler.Handle(
            new ChangePasswordCommand("missing", new ChangePasswordRequest
            {
                CurrentPassword = "Old@123",
                NewPassword = "New@123"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_ShouldFail_WhenCurrentPasswordInvalid()
    {
        var user = new ApplicationUser { Id = "u1", Email = "u1@identityhub.com", IsDeleted = false };
        var userManager = new StubUserManager
        {
            OnFindByIdAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnChangePasswordAsync = (_, _, _) => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "invalid" }))
        };

        var repository = new FakeAuthRepository();
        var audit = new FakeAuditLogRepository();
        var alerts = new FakeSecurityAlertService();

        var handler = new ChangePasswordCommandHandler(userManager, repository, alerts, audit);

        var result = await handler.Handle(
            new ChangePasswordCommand("u1", new ChangePasswordRequest
            {
                CurrentPassword = "Wrong@123",
                NewPassword = "New@123"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Password.ChangeFailed", result.Error?.Code);
        Assert.Equal(0, repository.SaveChangesCalls);
        Assert.Equal(0, audit.WriteCalls);
        Assert.Equal(0, alerts.CriticalActionCalls);
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_ShouldEndSessionsAndAuditAndNotify_WhenSuccessful()
    {
        var user = new ApplicationUser { Id = "u1", Email = "u1@identityhub.com", IsDeleted = false };
        var userManager = new StubUserManager
        {
            OnFindByIdAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnChangePasswordAsync = (_, _, _) => Task.FromResult(IdentityResult.Success)
        };

        var repository = new FakeAuthRepository();
        repository.ActiveSessionsByUserId[user.Id] =
        [
            new UserSession { Id = Guid.NewGuid(), UserId = user.Id, IsActive = true },
            new UserSession { Id = Guid.NewGuid(), UserId = user.Id, IsActive = true }
        ];

        var audit = new FakeAuditLogRepository();
        var alerts = new FakeSecurityAlertService();

        var handler = new ChangePasswordCommandHandler(userManager, repository, alerts, audit);

        var result = await handler.Handle(
            new ChangePasswordCommand("u1", new ChangePasswordRequest
            {
                CurrentPassword = "Old@123",
                NewPassword = "New@123"
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(repository.ActiveSessionsByUserId[user.Id], session => Assert.False(session.IsActive));
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal(1, audit.WriteCalls);
        Assert.Equal("Audit.User.PasswordChanged", audit.LastEventType);
        Assert.Equal(1, alerts.CriticalActionCalls);
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

    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        public Func<string, Task<ApplicationUser?>> OnFindByEmailAsync { get; set; }
            = _ => Task.FromResult<ApplicationUser?>(null);

        public Func<ApplicationUser, string, Task<bool>> OnCheckPasswordAsync { get; set; }
            = (_, _) => Task.FromResult(false);

        public Func<ApplicationUser, Task<IList<string>>> OnGetRolesAsync { get; set; }
            = _ => Task.FromResult<IList<string>>([]);

        public Func<ApplicationUser, Task<IList<System.Security.Claims.Claim>>> OnGetClaimsAsync { get; set; }
            = _ => Task.FromResult<IList<System.Security.Claims.Claim>>([]);

        public Func<string, Task<ApplicationUser?>> OnFindByIdAsync { get; set; }
            = _ => Task.FromResult<ApplicationUser?>(null);

        public Func<ApplicationUser, string, string, Task<IdentityResult>> OnChangePasswordAsync { get; set; }
            = (_, _, _) => Task.FromResult(IdentityResult.Success);

        public StubUserManager()
            : base(
                new StubUserStore(),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null,
                LoggerFactory.Create(_ => { }).CreateLogger<UserManager<ApplicationUser>>())
        {
        }

        public override Task<ApplicationUser?> FindByEmailAsync(string email)
            => OnFindByEmailAsync(email);

        public override Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
            => OnCheckPasswordAsync(user, password);

        public override Task<IList<string>> GetRolesAsync(ApplicationUser user)
            => OnGetRolesAsync(user);

        public override Task<IList<System.Security.Claims.Claim>> GetClaimsAsync(ApplicationUser user)
            => OnGetClaimsAsync(user);

        public override Task<ApplicationUser?> FindByIdAsync(string userId)
            => OnFindByIdAsync(userId);

        public override Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
            => OnChangePasswordAsync(user, currentPassword, newPassword);
    }

    private sealed class StubUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
            => Task.FromResult(IdentityResult.Success);

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
            => Task.FromResult<ApplicationUser?>(null);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
            => Task.FromResult<ApplicationUser?>(null);
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public Dictionary<string, List<UserSession>> ActiveSessionsByUserId { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int AddRefreshTokenCalls { get; private set; }
        public int AddSessionCalls { get; private set; }
        public int SaveChangesCalls { get; private set; }

        public UserSession? LastAddedSession { get; private set; }

        public Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
        {
            AddRefreshTokenCalls++;
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
            => Task.FromResult<RefreshToken?>(null);

        public Task<List<RefreshToken>> GetActiveRefreshTokensAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RefreshToken>());

        public Task<List<RefreshToken>> GetActiveRefreshTokensBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<RefreshToken>());

        public Task RevokeRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddSessionAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            AddSessionCalls++;
            LastAddedSession = session;
            return Task.CompletedTask;
        }

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

    private sealed class FakeDeviceInfoProvider : IClientDeviceInfoProvider
    {
        private readonly string _ip;
        private readonly string _browser;
        private readonly string _os;

        public FakeDeviceInfoProvider(string ip = "127.0.0.1", string browser = "TestBrowser", string os = "TestOS")
        {
            _ip = ip;
            _browser = browser;
            _os = os;
        }

        public (string IpAddress, string Browser, string OperatingSystem) GetCurrent()
            => (_ip, _browser, _os);
    }
}
