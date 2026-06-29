using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.CQRS.Auth.Handlers;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthCredentialHandlersUnitTests
{
    [Fact]
    public async Task ForgotPasswordCommandHandler_ShouldReturnSuccessWithoutEmail_WhenUserNotFound()
    {
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(null)
        };
        var emailService = new FakeEmailService();
        var templates = new FakeEmailTemplateBuilder();
        var config = BuildConfig("https://app.identityhub.local/");

        var handler = new ForgotPasswordCommandHandler(userManager, emailService, templates, config);

        var result = await handler.Handle(
            new ForgotPasswordCommand(new ForgotPasswordRequest { Email = "missing@identityhub.com" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, emailService.SendCalls);
        Assert.Equal(0, templates.ResetTemplateCalls);
    }

    [Fact]
    public async Task ForgotPasswordCommandHandler_ShouldBuildResetLinkAndSendEmail_WhenUserExists()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "john@identityhub.com",
            FullName = "John Doe",
            IsDeleted = false
        };

        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnGeneratePasswordResetTokenAsync = _ => Task.FromResult("reset-token-123")
        };

        var emailService = new FakeEmailService();
        var templates = new FakeEmailTemplateBuilder();
        var config = BuildConfig("https://portal.identityhub.local/");

        var handler = new ForgotPasswordCommandHandler(userManager, emailService, templates, config);

        var result = await handler.Handle(
            new ForgotPasswordCommand(new ForgotPasswordRequest { Email = "  JOHN@IDENTITYHUB.COM " }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, templates.ResetTemplateCalls);
        Assert.Contains("https://portal.identityhub.local/reset-password?email=john@identityhub.com&token=", templates.LastActionUrl);
        Assert.Equal("John Doe", templates.LastRecipientName);

        Assert.Equal(1, emailService.SendCalls);
        Assert.Equal("john@identityhub.com", emailService.LastTo);
        Assert.Equal("Reset your password", emailService.LastSubject);
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_ShouldFail_WhenUserNotFound()
    {
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(null)
        };
        var repository = new FakeAuthRepository();
        var alerts = new FakeSecurityAlertService();

        var handler = new ResetPasswordCommandHandler(userManager, repository, alerts);

        var result = await handler.Handle(
            new ResetPasswordCommand(new ResetPasswordRequest
            {
                Email = "missing@identityhub.com",
                Token = Encode("token"),
                NewPassword = "Password@123"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error?.Code);
        Assert.Equal(0, repository.SaveChangesCalls);
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_ShouldFail_WhenTokenFormatIsInvalid()
    {
        var user = new ApplicationUser { Id = "u1", Email = "u1@identityhub.com", IsDeleted = false };
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(user)
        };
        var repository = new FakeAuthRepository();
        var alerts = new FakeSecurityAlertService();

        var handler = new ResetPasswordCommandHandler(userManager, repository, alerts);

        var result = await handler.Handle(
            new ResetPasswordCommand(new ResetPasswordRequest
            {
                Email = user.Email!,
                Token = "not-base64-url",
                NewPassword = "Password@123"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Password.InvalidTokenFormat", result.Error?.Code);
        Assert.Equal(0, repository.SaveChangesCalls);
        Assert.Equal(0, alerts.CriticalActionCalls);
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_ShouldFail_WhenIdentityResetFails()
    {
        var user = new ApplicationUser { Id = "u1", Email = "u1@identityhub.com", IsDeleted = false };
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnResetPasswordAsync = (_, _, _) => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "invalid" }))
        };
        var repository = new FakeAuthRepository();
        var alerts = new FakeSecurityAlertService();

        var handler = new ResetPasswordCommandHandler(userManager, repository, alerts);

        var result = await handler.Handle(
            new ResetPasswordCommand(new ResetPasswordRequest
            {
                Email = user.Email!,
                Token = Encode("valid-token"),
                NewPassword = "Password@123"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Password.ResetFailed", result.Error?.Code);
        Assert.Equal(0, repository.SaveChangesCalls);
        Assert.Equal(0, alerts.CriticalActionCalls);
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_ShouldRevokeSessionsAndTokensAndNotify_WhenSuccessful()
    {
        var user = new ApplicationUser { Id = "u1", Email = "u1@identityhub.com", IsDeleted = false, IsActive = true };
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnResetPasswordAsync = (_, _, _) => Task.FromResult(IdentityResult.Success)
        };

        var repository = new FakeAuthRepository();
        repository.ActiveSessionsByUserId[user.Id] =
        [
            new UserSession { Id = Guid.NewGuid(), UserId = user.Id, IsActive = true },
            new UserSession { Id = Guid.NewGuid(), UserId = user.Id, IsActive = true }
        ];
        repository.ActiveRefreshTokensByUserId[user.Id] =
        [
            new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, IsRevoked = false },
            new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, IsRevoked = false }
        ];

        var alerts = new FakeSecurityAlertService();

        var handler = new ResetPasswordCommandHandler(userManager, repository, alerts);

        var result = await handler.Handle(
            new ResetPasswordCommand(new ResetPasswordRequest
            {
                Email = user.Email!,
                Token = Encode("valid-token"),
                NewPassword = "Password@123"
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(repository.ActiveSessionsByUserId[user.Id], session => Assert.False(session.IsActive));
        Assert.All(repository.ActiveRefreshTokensByUserId[user.Id], token => Assert.True(token.IsRevoked));
        Assert.Equal(1, repository.SaveChangesCalls);
        Assert.Equal(1, alerts.CriticalActionCalls);
    }

    private static IConfiguration BuildConfig(string frontendBaseUrl)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = frontendBaseUrl
            })
            .Build();
    }

    private static string Encode(string value)
        => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(value));

    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        public Func<string, Task<ApplicationUser?>> OnFindByEmailAsync { get; set; }
            = _ => Task.FromResult<ApplicationUser?>(null);

        public Func<ApplicationUser, Task<string>> OnGeneratePasswordResetTokenAsync { get; set; }
            = _ => Task.FromResult("reset-token");

        public Func<ApplicationUser, string, string, Task<IdentityResult>> OnResetPasswordAsync { get; set; }
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

        public override Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
            => OnGeneratePasswordResetTokenAsync(user);

        public override Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
            => OnResetPasswordAsync(user, token, newPassword);
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

    private sealed class FakeEmailService : IEmailService
    {
        public int SendCalls { get; private set; }
        public string LastTo { get; private set; } = string.Empty;
        public string LastSubject { get; private set; } = string.Empty;

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            SendCalls++;
            LastTo = to;
            LastSubject = subject;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailTemplateBuilder : IEmailTemplateBuilder
    {
        public int ResetTemplateCalls { get; private set; }
        public string LastActionUrl { get; private set; } = string.Empty;
        public string LastRecipientName { get; private set; } = string.Empty;

        public EmailTemplate BuildConfirmEmailTemplate(string actionUrl, string? recipientName = null)
            => new("Confirm email", "body");

        public EmailTemplate BuildResetPasswordTemplate(string actionUrl, string? recipientName = null)
        {
            ResetTemplateCalls++;
            LastActionUrl = actionUrl;
            LastRecipientName = recipientName ?? string.Empty;
            return new EmailTemplate("Reset your password", "body");
        }

        public EmailTemplate BuildInviteUserTemplate(string actionUrl, string? recipientName = null)
            => new("Invite", "body");

        public EmailTemplate BuildSuspiciousLoginAlertTemplate(string details, string? recipientName = null)
            => new("Suspicious", "body");

        public EmailTemplate BuildCriticalActionAlertTemplate(string actionTitle, string details, string? recipientName = null)
            => new("Critical", "body");
    }
}
