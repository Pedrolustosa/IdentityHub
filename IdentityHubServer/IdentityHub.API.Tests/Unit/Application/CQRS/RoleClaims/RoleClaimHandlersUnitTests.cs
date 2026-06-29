using System.Security.Claims;
using IdentityHub.Application.CQRS.RoleClaims.Commands;
using IdentityHub.Application.CQRS.RoleClaims.Handlers;
using IdentityHub.Application.CQRS.RoleClaims.Queries;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class RoleClaimHandlersUnitTests
{
    [Fact]
    public async Task AddRoleClaimPermissionCommandHandler_ShouldFailWhenDuplicateExists()
    {
        var repository = new FakeRoleRepository();
        var audit = new FakeAuditLogRepository();
        repository.Roles.Add(new IdentityRole("Admin") { Id = "r1" });
        repository.ClaimsByRoleId["r1"] = [new Claim("permission", "Users.View")];

        var handler = new AddRoleClaimPermissionCommandHandler(repository, audit);

        var result = await handler.Handle(new AddRoleClaimPermissionCommand("r1", " users.view "), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("RoleClaim.AlreadyExists", result.Error?.Code);
        Assert.Equal(0, repository.AddClaimCalls);
        Assert.Equal(0, audit.WriteCalls);
    }

    [Fact]
    public async Task AddRoleClaimPermissionCommandHandler_ShouldAddTrimmedPermissionAndAudit()
    {
        var repository = new FakeRoleRepository();
        var audit = new FakeAuditLogRepository();
        repository.Roles.Add(new IdentityRole("Admin") { Id = "r1" });

        var handler = new AddRoleClaimPermissionCommandHandler(repository, audit);

        var result = await handler.Handle(new AddRoleClaimPermissionCommand("r1", " Users.Update "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.AddClaimCalls);
        Assert.Equal("Users.Update", repository.LastAddedClaim?.Value);
        Assert.Equal("Audit.RoleClaim.Added", audit.LastEventType);
    }

    [Fact]
    public async Task RemoveRoleClaimPermissionCommandHandler_ShouldReturnSuccessWhenPermissionMissing()
    {
        var repository = new FakeRoleRepository();
        var audit = new FakeAuditLogRepository();
        repository.Roles.Add(new IdentityRole("Admin") { Id = "r1" });
        repository.ClaimsByRoleId["r1"] = [new Claim("permission", "Users.View")];

        var handler = new RemoveRoleClaimPermissionCommandHandler(repository, audit);

        var result = await handler.Handle(new RemoveRoleClaimPermissionCommand("r1", "Users.Update"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, repository.RemoveClaimCalls);
        Assert.Equal(0, audit.WriteCalls);
    }

    [Fact]
    public async Task ReplaceRoleClaimPermissionsCommandHandler_ShouldReplaceWithDistinctTrimmedPermissions()
    {
        var repository = new FakeRoleRepository();
        var audit = new FakeAuditLogRepository();
        repository.Roles.Add(new IdentityRole("Admin") { Id = "r1" });
        repository.ClaimsByRoleId["r1"] =
        [
            new Claim("permission", "Users.View"),
            new Claim("permission", "Roles.View")
        ];

        var handler = new ReplaceRoleClaimPermissionsCommandHandler(repository, audit);

        var result = await handler.Handle(
            new ReplaceRoleClaimPermissionsCommand("r1", [" Users.Update ", "users.update", "Roles.View", ""]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, repository.RemoveClaimCalls);
        Assert.Equal(2, repository.AddClaimCalls);
        var claims = repository.ClaimsByRoleId["r1"].Where(x => x.Type == "permission").Select(x => x.Value).ToList();
        Assert.Contains("Users.Update", claims);
        Assert.Contains("Roles.View", claims);
        Assert.Equal(2, claims.Count);
        Assert.Equal("Audit.RoleClaim.Replaced", audit.LastEventType);
    }

    [Fact]
    public async Task GetRoleClaimPermissionsQueryHandler_ShouldReturnDistinctSortedPermissions()
    {
        var repository = new FakeRoleRepository();
        repository.Roles.Add(new IdentityRole("Admin") { Id = "r1" });
        repository.ClaimsByRoleId["r1"] =
        [
            new Claim("permission", "users.view"),
            new Claim("permission", "Users.View"),
            new Claim("permission", "Roles.View"),
            new Claim("other", "x")
        ];

        var handler = new GetRoleClaimPermissionsQueryHandler(repository);

        var result = await handler.Handle(new GetRoleClaimPermissionsQuery("r1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Roles.View", "users.view" }, result.Value);
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        public List<IdentityRole> Roles { get; } = [];
        public Dictionary<string, List<Claim>> ClaimsByRoleId { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int AddClaimCalls { get; private set; }
        public int RemoveClaimCalls { get; private set; }
        public Claim? LastAddedClaim { get; private set; }

        public Task<List<IdentityRole>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.ToList());

        public Task<IdentityRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(x => x.Id == id));

        public Task<IdentityRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<int> GetUserCountAsync(string roleId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IDictionary<string, int>> GetUserCountsByRoleIdAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IDictionary<string, int>>(new Dictionary<string, int>());

        public Task CreateAsync(IdentityRole role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateAsync(IdentityRole role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(IdentityRole role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            ClaimsByRoleId.TryGetValue(role.Id, out var claims);
            return Task.FromResult<IList<Claim>>(claims?.ToList() ?? []);
        }

        public Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            AddClaimCalls++;
            LastAddedClaim = claim;

            if (!ClaimsByRoleId.TryGetValue(role.Id, out var claims))
            {
                claims = [];
                ClaimsByRoleId[role.Id] = claims;
            }

            claims.Add(claim);
            return Task.CompletedTask;
        }

        public Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            RemoveClaimCalls++;
            if (ClaimsByRoleId.TryGetValue(role.Id, out var claims))
            {
                var match = claims.FirstOrDefault(x => x.Type == claim.Type && x.Value == claim.Value);
                if (match is not null)
                    claims.Remove(match);
            }

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
