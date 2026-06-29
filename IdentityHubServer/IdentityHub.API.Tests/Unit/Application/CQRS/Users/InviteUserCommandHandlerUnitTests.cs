using IdentityHub.Application.CQRS.Users.Commands;
using IdentityHub.Application.CQRS.Users.Handlers;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class InviteUserCommandHandlerUnitTests
{
    [Fact]
    public async Task InviteUserCommandHandler_ShouldFail_WhenUserCreationFails()
    {
        var userRepo = new FakeUserRepository();
        var userManager = new StubUserManager
        {
            OnCreateAsync = _ => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "create failed" }))
        };

        var handler = CreateHandler(userRepo, userManager);

        var result = await handler.Handle(new InviteUserCommand(new InviteUserRequest
        {
            Email = "new@identityhub.com",
            FullName = "New User",
            IsActive = true,
            Roles = []
        }), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.CreateFailed", result.Error?.Code);
    }

    [Fact]
    public async Task InviteUserCommandHandler_ShouldFail_WhenExistingUserIsDeleted()
    {
        var userRepo = new FakeUserRepository();
        userRepo.Users.Add(new ApplicationUser
        {
            Id = "u1",
            Email = "deleted@identityhub.com",
            UserName = "deleted@identityhub.com",
            IsDeleted = true
        });

        var handler = CreateHandler(userRepo, new StubUserManager());

        var result = await handler.Handle(new InviteUserCommand(new InviteUserRequest
        {
            Email = "deleted@identityhub.com",
            FullName = "Deleted",
            IsActive = true,
            Roles = []
        }), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.Deleted", result.Error?.Code);
    }

    [Fact]
    public async Task InviteUserCommandHandler_ShouldFail_WhenExistingUserAlreadyHasPassword()
    {
        var userRepo = new FakeUserRepository();
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "exists@identityhub.com",
            UserName = "exists@identityhub.com",
            IsDeleted = false,
            IsActive = true
        };
        userRepo.Users.Add(user);

        var userManager = new StubUserManager
        {
            OnHasPasswordAsync = _ => Task.FromResult(true)
        };

        var handler = CreateHandler(userRepo, userManager);

        var result = await handler.Handle(new InviteUserCommand(new InviteUserRequest
        {
            Email = "exists@identityhub.com",
            FullName = "Exists",
            IsActive = true,
            Roles = ["User"]
        }), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.AlreadyExists", result.Error?.Code);
    }

    [Fact]
    public async Task InviteUserCommandHandler_ShouldInviteExistingUserWithoutPassword_UpdateDataAndRolesAndSendEmail()
    {
        var userRepo = new FakeUserRepository();
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "invite@identityhub.com",
            UserName = "invite@identityhub.com",
            FullName = "Old Name",
            IsDeleted = false,
            IsActive = true
        };
        userRepo.Users.Add(user);

        var userManager = new StubUserManager
        {
            OnHasPasswordAsync = _ => Task.FromResult(false),
            OnGetRolesAsync = _ => Task.FromResult<IList<string>>(["OldRole"]),
            OnRemoveFromRolesAsync = (_, _) => Task.FromResult(IdentityResult.Success),
            OnAddToRolesAsync = (_, roles) => Task.FromResult(roles.Contains("Manager") ? IdentityResult.Success : IdentityResult.Failed()),
            OnGeneratePasswordResetTokenAsync = _ => Task.FromResult("reset-token")
        };

        var email = new FakeEmailService();
        var template = new FakeEmailTemplateBuilder();
        var audit = new FakeAuditLogRepository();

        var handler = CreateHandler(userRepo, userManager, email, template, audit);

        var result = await handler.Handle(new InviteUserCommand(new InviteUserRequest
        {
            Email = " invite@identityhub.com ",
            FullName = "  New Name  ",
            IsActive = false,
            Roles = ["Manager"]
        }), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", user.FullName);
        Assert.False(user.IsActive);
        Assert.True(userRepo.UpdateCalls >= 1);
        Assert.Equal(1, userManager.RemoveRolesCalls);
        Assert.Equal(1, userManager.AddRolesCalls);
        Assert.Equal(1, email.SendCalls);
        Assert.Equal(1, audit.WriteCalls);

        Assert.Contains("/reset-password?email=invite%40identityhub.com&token=", template.LastActionUrl);
        var encoded = template.LastActionUrl.Split("token=")[1];
        var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
        Assert.Equal("reset-token", decoded);
    }

    private static InviteUserCommandHandler CreateHandler(
        FakeUserRepository userRepository,
        StubUserManager userManager,
        FakeEmailService? emailService = null,
        FakeEmailTemplateBuilder? templateBuilder = null,
        FakeAuditLogRepository? auditLogRepository = null)
    {
        return new InviteUserCommandHandler(
            userRepository,
            userManager,
            emailService ?? new FakeEmailService(),
            templateBuilder ?? new FakeEmailTemplateBuilder(),
            auditLogRepository ?? new FakeAuditLogRepository(),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Frontend:BaseUrl"] = "https://portal.identityhub.local/"
                })
                .Build());
    }

    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        public Func<ApplicationUser, Task<IdentityResult>> OnCreateAsync { get; set; }
            = _ => Task.FromResult(IdentityResult.Success);

        public Func<ApplicationUser, Task<bool>> OnHasPasswordAsync { get; set; }
            = _ => Task.FromResult(false);

        public Func<ApplicationUser, Task<IList<string>>> OnGetRolesAsync { get; set; }
            = _ => Task.FromResult<IList<string>>([]);

        public Func<ApplicationUser, IEnumerable<string>, Task<IdentityResult>> OnRemoveFromRolesAsync { get; set; }
            = (_, _) => Task.FromResult(IdentityResult.Success);

        public Func<ApplicationUser, IEnumerable<string>, Task<IdentityResult>> OnAddToRolesAsync { get; set; }
            = (_, _) => Task.FromResult(IdentityResult.Success);

        public Func<ApplicationUser, Task<string>> OnGeneratePasswordResetTokenAsync { get; set; }
            = _ => Task.FromResult("token");

        public int RemoveRolesCalls { get; private set; }
        public int AddRolesCalls { get; private set; }

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

        public override Task<IdentityResult> CreateAsync(ApplicationUser user)
            => OnCreateAsync(user);

        public override Task<bool> HasPasswordAsync(ApplicationUser user)
            => OnHasPasswordAsync(user);

        public override Task<IList<string>> GetRolesAsync(ApplicationUser user)
            => OnGetRolesAsync(user);

        public override Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles)
        {
            RemoveRolesCalls++;
            return OnRemoveFromRolesAsync(user, roles);
        }

        public override Task<IdentityResult> AddToRolesAsync(ApplicationUser user, IEnumerable<string> roles)
        {
            AddRolesCalls++;
            return OnAddToRolesAsync(user, roles);
        }

        public override Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
            => OnGeneratePasswordResetTokenAsync(user);
    }

    private sealed class StubUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<ApplicationUser> Users { get; } = [];
        public int UpdateCalls { get; private set; }

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
            => Task.CompletedTask;

        public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ApplicationUser user, string? deletedBy, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateRolesAsync(ApplicationUser user, IList<string> roles, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public int SendCalls { get; private set; }

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            SendCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailTemplateBuilder : IEmailTemplateBuilder
    {
        public string LastActionUrl { get; private set; } = string.Empty;

        public EmailTemplate BuildConfirmEmailTemplate(string actionUrl, string? recipientName = null)
            => new("Confirm", "body");

        public EmailTemplate BuildResetPasswordTemplate(string actionUrl, string? recipientName = null)
            => new("Reset", "body");

        public EmailTemplate BuildInviteUserTemplate(string actionUrl, string? recipientName = null)
        {
            LastActionUrl = actionUrl;
            return new EmailTemplate("Invite", "body");
        }

        public EmailTemplate BuildSuspiciousLoginAlertTemplate(string details, string? recipientName = null)
            => new("Suspicious", "body");

        public EmailTemplate BuildCriticalActionAlertTemplate(string actionTitle, string details, string? recipientName = null)
            => new("Critical", "body");
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public int WriteCalls { get; private set; }

        public Task<(IReadOnlyList<AuditLogEntry> Items, int TotalCount)> GetPagedAsync(AuditLogFilter request, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AuditLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<AuditLogEntry>> GetRecentByUserAsync(string userId, int take, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task WriteAsync(string eventType, string description, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            return Task.CompletedTask;
        }

        public Task WriteAsync(string eventType, string description, string? targetId, object? metadata, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            return Task.CompletedTask;
        }
    }
}
