using IdentityHub.Domain.Constants;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class StartupSeedSynchronizationIntegrationTests
{
    [Fact]
    public async Task Startup_ShouldResyncRolePermissions_WhenUsersAlreadyExist()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"identityhub-seed-{Guid.NewGuid():N}.db");

        var additionalConfiguration = new Dictionary<string, string?>
        {
            ["Tests:SqliteFilePath"] = dbPath
        };

        try
        {
            using (var initialFactory = new TestWebApplicationFactory(additionalConfiguration))
            {
                using var initialClient = initialFactory.CreateClient();

                using var initialScope = initialFactory.Services.CreateScope();
                var db = initialScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var adminRoleId = await db.Roles
                    .Where(role => role.Name == "Admin")
                    .Select(role => role.Id)
                    .SingleAsync();

                var claimToRemove = await db.Set<IdentityRoleClaim<string>>()
                    .FirstOrDefaultAsync(c =>
                        c.RoleId == adminRoleId &&
                        c.ClaimType == "permission" &&
                        c.ClaimValue == AppPermissions.Users.InvitesView);

                Assert.NotNull(claimToRemove);

                db.Remove(claimToRemove!);
                await db.SaveChangesAsync();

                var existsAfterRemove = await db.Set<IdentityRoleClaim<string>>()
                    .AnyAsync(c =>
                        c.RoleId == adminRoleId &&
                        c.ClaimType == "permission" &&
                        c.ClaimValue == AppPermissions.Users.InvitesView);

                Assert.False(existsAfterRemove);
            }

            using (var restartedFactory = new TestWebApplicationFactory(additionalConfiguration))
            {
                using var restartedClient = restartedFactory.CreateClient();

                using var restartedScope = restartedFactory.Services.CreateScope();
                var restartedDb = restartedScope.ServiceProvider.GetRequiredService<AppDbContext>();

                var adminRoleId = await restartedDb.Roles
                    .Where(role => role.Name == "Admin")
                    .Select(role => role.Id)
                    .SingleAsync();

                var existsAfterRestart = await restartedDb.Set<IdentityRoleClaim<string>>()
                    .AnyAsync(c =>
                        c.RoleId == adminRoleId &&
                        c.ClaimType == "permission" &&
                        c.ClaimValue == AppPermissions.Users.InvitesView);

                Assert.True(existsAfterRestart);
            }
        }
        finally
        {
            // On Windows the sqlite file handle may be released a moment after host disposal.
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (File.Exists(dbPath))
                    {
                        File.Delete(dbPath);
                    }

                    break;
                }
                catch (IOException) when (attempt < 4)
                {
                    await Task.Delay(100);
                }
                catch (IOException)
                {
                    // Best-effort cleanup only.
                    break;
                }
            }
        }
    }
}
