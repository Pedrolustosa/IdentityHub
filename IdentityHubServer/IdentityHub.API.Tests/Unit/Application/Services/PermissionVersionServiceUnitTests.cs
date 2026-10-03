using IdentityHub.Application.Services;
using IdentityHub.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class PermissionVersionServiceUnitTests
{
    [Fact]
    public async Task BumpUserAsync_ShouldIncrementPermissionVersionAndPersist()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "u1@identityhub.com", PermissionVersion = 3 };
        var userManager = new StubUserManager { UsersById = { ["u1"] = user } };
        var service = new PermissionVersionService(userManager);

        await service.BumpUserAsync(user, CancellationToken.None);

        Assert.Equal(4, user.PermissionVersion);
        Assert.Equal(1, userManager.UpdateCalls);
    }

    [Fact]
    public async Task BumpUsersInRoleAsync_ShouldBumpEveryMember()
    {
        var alice = new ApplicationUser { Id = "a", UserName = "a@identityhub.com", PermissionVersion = 1 };
        var bob = new ApplicationUser { Id = "b", UserName = "b@identityhub.com", PermissionVersion = 5 };
        var userManager = new StubUserManager
        {
            UsersByRoleName =
            {
                ["Manager"] = [alice, bob]
            },
            UsersById =
            {
                ["a"] = alice,
                ["b"] = bob
            }
        };
        var service = new PermissionVersionService(userManager);

        await service.BumpUsersInRoleAsync("Manager", CancellationToken.None);

        Assert.Equal(2, alice.PermissionVersion);
        Assert.Equal(6, bob.PermissionVersion);
        Assert.Equal(2, userManager.UpdateCalls);
    }

    [Fact]
    public async Task BumpUsersInRoleAsync_ShouldNoOp_WhenRoleNameMissing()
    {
        var userManager = new StubUserManager();
        var service = new PermissionVersionService(userManager);

        await service.BumpUsersInRoleAsync("  ", CancellationToken.None);

        Assert.Equal(0, userManager.UpdateCalls);
        Assert.Equal(0, userManager.GetUsersInRoleCalls);
    }

    private sealed class StubUserManager : UserManager<ApplicationUser>
    {
        public Dictionary<string, ApplicationUser> UsersById { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<ApplicationUser>> UsersByRoleName { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int UpdateCalls { get; private set; }
        public int GetUsersInRoleCalls { get; private set; }

        public StubUserManager()
            : base(
                new StubUserStore(),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                new PasswordHasher<ApplicationUser>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                LoggerFactory.Create(_ => { }).CreateLogger<UserManager<ApplicationUser>>())
        {
        }

        public override Task<IdentityResult> UpdateAsync(ApplicationUser user)
        {
            UpdateCalls++;
            UsersById[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public override Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName)
        {
            GetUsersInRoleCalls++;
            UsersByRoleName.TryGetValue(roleName, out var users);
            return Task.FromResult<IList<ApplicationUser>>(users?.ToList() ?? []);
        }
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
}
