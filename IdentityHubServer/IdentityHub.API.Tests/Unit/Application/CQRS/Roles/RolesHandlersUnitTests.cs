using System.Security.Claims;
using IdentityHub.Application.CQRS.Roles.Commands;
using IdentityHub.Application.CQRS.Roles.Handlers;
using IdentityHub.Application.CQRS.Roles.Queries;
using IdentityHub.Application.DTOs;
using IdentityHub.Domain.Entities;
using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class RolesHandlersUnitTests
{
    [Fact]
    public async Task CreateRoleCommandHandler_ShouldReturnFailure_WhenNameIsEmpty()
    {
        var repository = new FakeRoleRepository();
        var audit = new FakeAuditLogRepository();
        var handler = new CreateRoleCommandHandler(repository, audit);

        var result = await handler.Handle(
            new CreateRoleCommand(new CreateRoleRequest { Name = "  " }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Role.NameRequired", result.Error?.Code);
        Assert.Equal(0, repository.CreateCalls);
        Assert.Equal(0, audit.WriteCalls);
    }

    [Fact]
    public async Task CreateRoleCommandHandler_ShouldCreateAndAudit_WhenRoleDoesNotExist()
    {
        var repository = new FakeRoleRepository();
        var audit = new FakeAuditLogRepository();
        var handler = new CreateRoleCommandHandler(repository, audit);

        var result = await handler.Handle(
            new CreateRoleCommand(new CreateRoleRequest { Name = " Managers " }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.CreateCalls);
        Assert.Single(repository.Roles);
        Assert.Equal("Managers", repository.Roles[0].Name);
        Assert.Equal(1, audit.WriteCalls);
        Assert.Equal("Audit.Role.Created", audit.LastEventType);
        Assert.Contains("name=Managers", audit.LastDescription);
    }

    [Fact]
    public async Task DeleteRoleCommandHandler_ShouldBlockAdminDeletion()
    {
        var repository = new FakeRoleRepository();
        var audit = new FakeAuditLogRepository();
        repository.Roles.Add(new IdentityRole("Admin") { Id = "admin-role" });

        var handler = new DeleteRoleCommandHandler(repository, audit);

        var result = await handler.Handle(new DeleteRoleCommand("admin-role"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Role.AdminCannotBeDeleted", result.Error?.Code);
        Assert.Equal(0, repository.DeleteCalls);
        Assert.Equal(0, audit.WriteCalls);
    }

    [Fact]
    public async Task GetRoleByIdQueryHandler_ShouldReturnMappedResponse()
    {
        var repository = new FakeRoleRepository();
        repository.Roles.Add(new IdentityRole("Operator") { Id = "role-1" });
        repository.UserCountsByRoleId["role-1"] = 3;

        var handler = new GetRoleByIdQueryHandler(repository);

        var result = await handler.Handle(new GetRoleByIdQuery("role-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("role-1", result.Value!.Id);
        Assert.Equal("Operator", result.Value.Name);
        Assert.Equal(3, result.Value.UserCount);
    }

    [Fact]
    public async Task GetRolesQueryHandler_ShouldMapCountsForAllRoles()
    {
        var repository = new FakeRoleRepository();
        repository.Roles.Add(new IdentityRole("Admin") { Id = "r1" });
        repository.Roles.Add(new IdentityRole("User") { Id = "r2" });
        repository.UserCountsByRoleId["r1"] = 2;

        var handler = new GetRolesQueryHandler(repository);

        var result = await handler.Handle(new GetRolesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(2, result.Value.Single(x => x.Id == "r1").UserCount);
        Assert.Equal(0, result.Value.Single(x => x.Id == "r2").UserCount);
    }

    [Fact]
    public async Task GetRolePermissionsQueryHandler_ShouldReturnDistinctSortedPermissions()
    {
        var repository = new FakeRoleRepository();
        repository.Roles.Add(new IdentityRole("Reader") { Id = "r1" });
        repository.ClaimsByRoleId["r1"] =
        [
            new Claim("permission", "users.view"),
            new Claim("permission", "Users.View"),
            new Claim("permission", "Roles.View"),
            new Claim("other", "ignored")
        ];

        var handler = new GetRolePermissionsQueryHandler(repository);

        var result = await handler.Handle(new GetRolePermissionsQuery("r1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Roles.View", "users.view" }, result.Value);
    }

    [Fact]
    public async Task GetPermissionCatalogQueryHandler_ShouldReturnDistinctSortedCatalog()
    {
        var handler = new GetPermissionCatalogQueryHandler();

        var result = await handler.Handle(new GetPermissionCatalogQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value!);
        var sorted = result.Value!.OrderBy(x => x).ToList();
        Assert.Equal(sorted, result.Value);
        Assert.Equal(result.Value.Distinct(StringComparer.OrdinalIgnoreCase).Count(), result.Value.Count);
    }

    private sealed class FakeRoleRepository : IRoleRepository
    {
        public List<IdentityRole> Roles { get; } = [];
        public Dictionary<string, List<Claim>> ClaimsByRoleId { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> UserCountsByRoleId { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int CreateCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task<List<IdentityRole>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.ToList());

        public Task<IdentityRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(x => x.Id == id));

        public Task<IdentityRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<int> GetUserCountAsync(string roleId, CancellationToken cancellationToken = default)
            => Task.FromResult(UserCountsByRoleId.TryGetValue(roleId, out var count) ? count : 0);

        public Task<IDictionary<string, int>> GetUserCountsByRoleIdAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IDictionary<string, int>>(new Dictionary<string, int>(UserCountsByRoleId));

        public Task CreateAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            if (string.IsNullOrWhiteSpace(role.Id))
                role.Id = Guid.NewGuid().ToString();
            Roles.Add(role);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            Roles.RemoveAll(x => x.Id == role.Id);
            return Task.CompletedTask;
        }

        public Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = default)
        {
            ClaimsByRoleId.TryGetValue(role.Id, out var claims);
            return Task.FromResult<IList<Claim>>(claims?.ToList() ?? []);
        }

        public Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = default)
        {
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
        public string LastDescription { get; private set; } = string.Empty;

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
            LastDescription = description;
            return Task.CompletedTask;
        }

        public Task WriteAsync(string eventType, string description, string? targetId, object? metadata, CancellationToken cancellationToken = default)
        {
            WriteCalls++;
            LastEventType = eventType;
            LastDescription = description;
            return Task.CompletedTask;
        }
    }
}
