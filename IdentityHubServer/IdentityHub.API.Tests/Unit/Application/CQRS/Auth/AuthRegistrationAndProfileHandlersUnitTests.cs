using IdentityHub.Application.CQRS.Auth.Commands;
using IdentityHub.Application.CQRS.Auth.Handlers;
using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthRegistrationAndProfileHandlersUnitTests
{
    [Fact]
    public async Task RegisterCommandHandler_ShouldFail_WhenUserAlreadyExists()
    {
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(new ApplicationUser { Id = "u1" })
        };

        var handler = new RegisterCommandHandler(
            userManager,
            new FakeEmailService(),
            new FakeEmailTemplateBuilder(),
            BuildConfig("https://portal.identityhub.local/"));

        var result = await handler.Handle(
            new RegisterCommand(new RegisterRequest
            {
                Email = "existing@identityhub.com",
                Password = "Password@123",
                FullName = "Existing"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.Exists", result.Error?.Code);
    }

    [Fact]
    public async Task RegisterCommandHandler_ShouldFail_WhenCreateFails()
    {
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(null),
            OnCreateWithPasswordAsync = (_, _) => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "weak" }))
        };

        var handler = new RegisterCommandHandler(
            userManager,
            new FakeEmailService(),
            new FakeEmailTemplateBuilder(),
            BuildConfig("https://portal.identityhub.local/"));

        var result = await handler.Handle(
            new RegisterCommand(new RegisterRequest
            {
                Email = "new@identityhub.com",
                Password = "123",
                FullName = "New"
            }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.CreateFailed", result.Error?.Code);
    }

    [Fact]
    public async Task RegisterCommandHandler_ShouldNormalizeEmail_AndSendConfirmationEmail_WhenSuccessful()
    {
        var userManager = new StubUserManager
        {
            OnFindByEmailAsync = _ => Task.FromResult<ApplicationUser?>(null),
            OnCreateWithPasswordAsync = (_, _) => Task.FromResult(IdentityResult.Success),
            OnGenerateEmailConfirmationTokenAsync = _ => Task.FromResult("confirm-token")
        };
        var emailService = new FakeEmailService();
        var templates = new FakeEmailTemplateBuilder();

        var handler = new RegisterCommandHandler(
            userManager,
            emailService,
            templates,
            BuildConfig("https://portal.identityhub.local/"));

        var result = await handler.Handle(
            new RegisterCommand(new RegisterRequest
            {
                Email = "  New.User@IdentityHub.COM ",
                Password = "Password@123",
                FullName = "New User"
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("new.user@identityhub.com", userManager.LastCreatedUser?.Email);
        Assert.Equal("new.user@identityhub.com", userManager.LastCreatedUser?.UserName);
        Assert.Equal(1, templates.ConfirmCalls);
        Assert.Contains("https://portal.identityhub.local/confirm-email?email=new.user@identityhub.com&token=", templates.LastActionUrl);

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(templates.LastActionUrl.Split("token=")[1]));
        Assert.Equal("confirm-token", decodedToken);

        Assert.Equal(1, emailService.SendCalls);
        Assert.Equal("new.user@identityhub.com", emailService.LastTo);
    }

    [Fact]
    public async Task UpdateProfileCommandHandler_ShouldFail_WhenUserNotFound()
    {
        var userManager = new StubUserManager
        {
            OnFindByIdAsync = _ => Task.FromResult<ApplicationUser?>(null)
        };

        var handler = new UpdateProfileCommandHandler(userManager);

        var result = await handler.Handle(
            new UpdateProfileCommand("missing", new UpdateProfileRequest { FullName = "Any", Email = "a@a.com" }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task UpdateProfileCommandHandler_ShouldTrimNameAndUpdate_WhenUserExists()
    {
        var user = new ApplicationUser
        {
            Id = "u1",
            Email = "u1@identityhub.com",
            FullName = "Old",
            IsDeleted = false
        };

        var userManager = new StubUserManager
        {
            OnFindByIdAsync = _ => Task.FromResult<ApplicationUser?>(user),
            OnUpdateAsync = _ => Task.FromResult(IdentityResult.Success)
        };

        var handler = new UpdateProfileCommandHandler(userManager);

        var result = await handler.Handle(
            new UpdateProfileCommand("u1", new UpdateProfileRequest
            {
                FullName = "  New Name  ",
                Email = "ignore@identityhub.com"
            }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", user.FullName);
        Assert.Equal(1, userManager.UpdateCalls);
    }

    private static IConfiguration BuildConfig(string baseUrl)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Frontend:BaseUrl"] = baseUrl
            })
            .Build();
    }

    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        public Func<string, Task<ApplicationUser?>> OnFindByEmailAsync { get; set; }
            = _ => Task.FromResult<ApplicationUser?>(null);

        public Func<ApplicationUser, string, Task<IdentityResult>> OnCreateWithPasswordAsync { get; set; }
            = (_, _) => Task.FromResult(IdentityResult.Success);

        public Func<ApplicationUser, Task<string>> OnGenerateEmailConfirmationTokenAsync { get; set; }
            = _ => Task.FromResult("token");

        public Func<string, Task<ApplicationUser?>> OnFindByIdAsync { get; set; }
            = _ => Task.FromResult<ApplicationUser?>(null);

        public Func<ApplicationUser, Task<IdentityResult>> OnUpdateAsync { get; set; }
            = _ => Task.FromResult(IdentityResult.Success);

        public ApplicationUser? LastCreatedUser { get; private set; }
        public int UpdateCalls { get; private set; }

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

        public override Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
        {
            LastCreatedUser = user;
            return OnCreateWithPasswordAsync(user, password);
        }

        public override Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
            => OnGenerateEmailConfirmationTokenAsync(user);

        public override Task<ApplicationUser?> FindByIdAsync(string userId)
            => OnFindByIdAsync(userId);

        public override Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            UpdateCalls++;
            return OnUpdateAsync(user);
        }
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

    private sealed class FakeEmailService : IEmailService
    {
        public int SendCalls { get; private set; }
        public string LastTo { get; private set; } = string.Empty;

        public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            SendCalls++;
            LastTo = to;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailTemplateBuilder : IEmailTemplateBuilder
    {
        public int ConfirmCalls { get; private set; }
        public string LastActionUrl { get; private set; } = string.Empty;

        public EmailTemplate BuildConfirmEmailTemplate(string actionUrl, string? recipientName = null)
        {
            ConfirmCalls++;
            LastActionUrl = actionUrl;
            return new EmailTemplate("Confirm", "body");
        }

        public EmailTemplate BuildResetPasswordTemplate(string actionUrl, string? recipientName = null)
            => new("Reset", "body");

        public EmailTemplate BuildInviteUserTemplate(string actionUrl, string? recipientName = null)
            => new("Invite", "body");

        public EmailTemplate BuildSuspiciousLoginAlertTemplate(string details, string? recipientName = null)
            => new("Suspicious", "body");

        public EmailTemplate BuildCriticalActionAlertTemplate(string actionTitle, string details, string? recipientName = null)
            => new("Critical", "body");
    }
}
