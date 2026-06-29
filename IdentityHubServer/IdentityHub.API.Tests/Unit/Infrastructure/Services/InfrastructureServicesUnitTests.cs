using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using IdentityHub.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class InfrastructureServicesUnitTests
{
    [Fact]
    public async Task UserInvitesService_GetUserInvitesAsync_ShouldReturnOnlyActiveOrderedAndPaged()
    {
        await using var scope = await SqliteDbScope.CreateAsync();

        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);

        scope.DbContext.UserInvites.AddRange(
            new UserInvite
            {
                Id = Guid.NewGuid(),
                Email = "active-older@identityhub.com",
                IsActive = true,
                SentAt = older,
                Status = "Pending",
                Roles = "User"
            },
            new UserInvite
            {
                Id = Guid.NewGuid(),
                Email = "inactive@identityhub.com",
                IsActive = false,
                SentAt = DateTime.UtcNow,
                Status = "Canceled",
                Roles = "Manager"
            },
            new UserInvite
            {
                Id = Guid.NewGuid(),
                Email = "active-newer@identityhub.com",
                IsActive = true,
                SentAt = newer,
                Status = "Pending",
                Roles = "Admin"
            });

        await scope.DbContext.SaveChangesAsync();

        var service = new UserInvitesService(scope.DbContext);

        var result = await service.GetUserInvitesAsync(page: 1, pageSize: 1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value!.Items);
        Assert.Equal("active-newer@identityhub.com", result.Value.Items[0].Email);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.TotalPages);
    }

    [Fact]
    public async Task UserInvitesService_ResendUserInviteAsync_ShouldFail_WhenInviteNotFound()
    {
        await using var scope = await SqliteDbScope.CreateAsync();
        var service = new UserInvitesService(scope.DbContext);

        var result = await service.ResendUserInviteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invite.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task UserInvitesService_ResendUserInviteAsync_ShouldUpdateSentAndExpiration_WhenFound()
    {
        await using var scope = await SqliteDbScope.CreateAsync();

        var invite = new UserInvite
        {
            Id = Guid.NewGuid(),
            Email = "resend@identityhub.com",
            IsActive = true,
            SentAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-3),
            Status = "Pending"
        };

        scope.DbContext.UserInvites.Add(invite);
        await scope.DbContext.SaveChangesAsync();

        var service = new UserInvitesService(scope.DbContext);

        var result = await service.ResendUserInviteAsync(invite.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await scope.DbContext.UserInvites.SingleAsync(x => x.Id == invite.Id);
        Assert.True(updated.SentAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.NotNull(updated.ExpiresAt);
        Assert.True(updated.ExpiresAt > DateTime.UtcNow.AddDays(6));
    }

    [Fact]
    public async Task UserInvitesService_CancelUserInviteAsync_ShouldFail_WhenInviteNotFound()
    {
        await using var scope = await SqliteDbScope.CreateAsync();
        var service = new UserInvitesService(scope.DbContext);

        var result = await service.CancelUserInviteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Invite.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task UserInvitesService_CancelUserInviteAsync_ShouldSetCanceledState_WhenFound()
    {
        await using var scope = await SqliteDbScope.CreateAsync();

        var invite = new UserInvite
        {
            Id = Guid.NewGuid(),
            Email = "cancel@identityhub.com",
            IsActive = true,
            SentAt = DateTime.UtcNow,
            Status = "Pending"
        };

        scope.DbContext.UserInvites.Add(invite);
        await scope.DbContext.SaveChangesAsync();

        var service = new UserInvitesService(scope.DbContext);

        var result = await service.CancelUserInviteAsync(invite.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await scope.DbContext.UserInvites.SingleAsync(x => x.Id == invite.Id);
        Assert.Equal("Canceled", updated.Status);
        Assert.False(updated.IsActive);
        Assert.NotNull(updated.CanceledAt);
    }

    [Fact]
    public async Task SecuritySettingsService_GetSettingsAsync_ShouldReturnDefaults_WhenNoRecordExists()
    {
        await using var scope = await SqliteDbScope.CreateAsync();
        var service = new SecuritySettingsService(scope.DbContext);

        var result = await service.GetSettingsAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(30, result.Value!.AccessTokenMinutes);
        Assert.Equal(7, result.Value.RefreshTokenDays);
        Assert.Equal(5, result.Value.MaxLoginAttempts);
        Assert.Equal(15, result.Value.LockDurationMinutes);
        Assert.True(result.Value.RequireEmailConfirmation);
    }

    [Fact]
    public async Task SecuritySettingsService_GetSettingsAsync_ShouldReturnPersistedValues_WhenRecordExists()
    {
        await using var scope = await SqliteDbScope.CreateAsync();

        scope.DbContext.SecuritySettings.Add(new SecuritySetting
        {
            Id = Guid.NewGuid(),
            AccessTokenMinutes = 45,
            RefreshTokenDays = 10,
            MaxLoginAttempts = 9,
            LockDurationMinutes = 22,
            RequireEmailConfirmation = false,
            CreatedAt = DateTime.UtcNow
        });
        await scope.DbContext.SaveChangesAsync();

        var service = new SecuritySettingsService(scope.DbContext);

        var result = await service.GetSettingsAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value!.AccessTokenMinutes);
        Assert.Equal(10, result.Value.RefreshTokenDays);
        Assert.Equal(9, result.Value.MaxLoginAttempts);
        Assert.Equal(22, result.Value.LockDurationMinutes);
        Assert.False(result.Value.RequireEmailConfirmation);
    }

    [Fact]
    public async Task SecuritySettingsService_UpdateSettingsAsync_ShouldCreate_WhenNoRecordExists()
    {
        await using var scope = await SqliteDbScope.CreateAsync();
        var service = new SecuritySettingsService(scope.DbContext);

        var request = new UpdateSecuritySettingsRequest
        {
            AccessTokenMinutes = 20,
            RefreshTokenDays = 11,
            MaxLoginAttempts = 6,
            LockDurationMinutes = 18,
            RequireEmailConfirmation = false
        };

        var result = await service.UpdateSettingsAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = await scope.DbContext.SecuritySettings.SingleAsync();
        Assert.Equal(20, created.AccessTokenMinutes);
        Assert.Equal(11, created.RefreshTokenDays);
        Assert.Equal(6, created.MaxLoginAttempts);
        Assert.Equal(18, created.LockDurationMinutes);
        Assert.False(created.RequireEmailConfirmation);
        Assert.NotEqual(default, created.CreatedAt);
    }

    [Fact]
    public async Task SecuritySettingsService_UpdateSettingsAsync_ShouldUpdate_WhenRecordExists()
    {
        await using var scope = await SqliteDbScope.CreateAsync();

        var existing = new SecuritySetting
        {
            Id = Guid.NewGuid(),
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7,
            MaxLoginAttempts = 5,
            LockDurationMinutes = 15,
            RequireEmailConfirmation = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        scope.DbContext.SecuritySettings.Add(existing);
        await scope.DbContext.SaveChangesAsync();

        var service = new SecuritySettingsService(scope.DbContext);
        var request = new UpdateSecuritySettingsRequest
        {
            AccessTokenMinutes = 60,
            RefreshTokenDays = 14,
            MaxLoginAttempts = 8,
            LockDurationMinutes = 25,
            RequireEmailConfirmation = false
        };

        var result = await service.UpdateSettingsAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await scope.DbContext.SecuritySettings.SingleAsync();
        Assert.Equal(existing.Id, updated.Id);
        Assert.Equal(60, updated.AccessTokenMinutes);
        Assert.Equal(14, updated.RefreshTokenDays);
        Assert.Equal(8, updated.MaxLoginAttempts);
        Assert.Equal(25, updated.LockDurationMinutes);
        Assert.False(updated.RequireEmailConfirmation);
        Assert.NotNull(updated.UpdatedAt);
    }

    private sealed class SqliteDbScope : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext DbContext { get; }

        private SqliteDbScope(SqliteConnection connection, AppDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public static async Task<SqliteDbScope> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new SqliteDbScope(connection, dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
