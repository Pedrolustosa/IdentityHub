using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuditLogIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuditLogIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task UserCreateUpdateDelete_ShouldPersistAuditEvents()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var email = $"audit.user.{marker}@identityhub.com";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Audit@123",
            fullName = "Audit User"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();

        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListDto>>();
        var createdUser = users?.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(createdUser);

        var updateResponse = await _client.PutAsJsonAsync($"/api/users/{createdUser!.Id}", new
        {
            fullName = "Audit User Updated",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var events = await db.AuditLogEntries
            .Where(e => e.Description.Contains(email))
            .Select(e => e.Type)
            .ToListAsync();

        Assert.Contains("Audit.User.Created", events);
        Assert.Contains("Audit.User.Updated", events);
        Assert.Contains("Audit.User.Deleted", events);

        var userIds = await db.AuditLogEntries
            .Where(e => e.Description.Contains(email))
            .Select(e => e.ActorUserId)
            .ToListAsync();

        Assert.All(userIds, id => Assert.False(string.IsNullOrWhiteSpace(id)));
        Assert.DoesNotContain("system", userIds);
    }

    [Fact]
    public async Task RoleAndClaimsChanges_ShouldPersistAuditEvents()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var roleName = $"AuditRole{marker}";

        var createRoleResponse = await _client.PostAsJsonAsync("/api/roles", new { name = roleName });
        Assert.Equal(HttpStatusCode.OK, createRoleResponse.StatusCode);

        var rolesResponse = await _client.GetAsync("/api/roles");
        rolesResponse.EnsureSuccessStatusCode();

        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleDto>>();
        var role = roles?.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(role);

        var updatedRoleName = $"{roleName}Updated";

        var updateRoleResponse = await _client.PutAsJsonAsync($"/api/roles/{role!.Id}", new { name = updatedRoleName });
        Assert.Equal(HttpStatusCode.OK, updateRoleResponse.StatusCode);

        var updatePermissionsResponse = await _client.PutAsJsonAsync($"/api/roles/{role.Id}/permissions", new
        {
            permissions = new[] { "Dashboard.View" }
        });
        Assert.Equal(HttpStatusCode.OK, updatePermissionsResponse.StatusCode);

        var addClaimResponse = await _client.PostAsJsonAsync($"/api/role-claims/{role.Id}", "Users.View");
        Assert.Equal(HttpStatusCode.OK, addClaimResponse.StatusCode);

        var replaceClaimsResponse = await _client.PutAsJsonAsync($"/api/role-claims/{role.Id}", new[] { "Users.View", "Users.Update" });
        Assert.Equal(HttpStatusCode.OK, replaceClaimsResponse.StatusCode);

        var removeClaimResponse = await _client.DeleteAsync($"/api/role-claims/{role.Id}?permission=Users.Update");
        Assert.Equal(HttpStatusCode.OK, removeClaimResponse.StatusCode);

        var deleteRoleResponse = await _client.DeleteAsync($"/api/roles/{role.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteRoleResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var eventTypes = await db.AuditLogEntries
            .Where(e => e.Description.Contains(role.Id))
            .Select(e => e.Type)
            .ToListAsync();

        Assert.Contains("Audit.Role.Created", eventTypes);
        Assert.Contains("Audit.Role.Updated", eventTypes);
        Assert.Contains("Audit.Role.PermissionsUpdated", eventTypes);
        Assert.Contains("Audit.RoleClaim.Added", eventTypes);
        Assert.Contains("Audit.RoleClaim.Replaced", eventTypes);
        Assert.Contains("Audit.RoleClaim.Removed", eventTypes);
        Assert.Contains("Audit.Role.Deleted", eventTypes);
    }

    [Fact]
    public async Task GetAuditLogs_ShouldReturnPagedEntries()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var email = $"audit.endpoint.{marker}@identityhub.com";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Audit@123",
            fullName = "Audit Endpoint User"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var response = await _client.GetAsync("/api/audit-logs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedAuditLogResponseDto>();

        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(10, payload.PageSize);
        Assert.True(payload.TotalCount >= 1);
        Assert.Contains(payload.Items, x => x.Description.Contains(email));
    }

    [Fact]
    public async Task GetAuditLogs_ShouldApplyTypeActorDescriptionAndPeriodFilters()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var email = $"audit.filter.{marker}@identityhub.com";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Audit@123",
            fullName = "Audit Filter User"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actorUserId = await db.AuditLogEntries
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => x.ActorUserId)
            .FirstAsync();

        var fromDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync(
            $"/api/audit-logs?page=1&pageSize=10&type=Audit.User.Created&actorUserId={Uri.EscapeDataString(actorUserId)}&description={Uri.EscapeDataString(email)}&fromDate={fromDate}&toDate={toDate}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedAuditLogResponseDto>();

        Assert.NotNull(payload);
        Assert.NotEmpty(payload!.Items);
        Assert.All(payload.Items, item =>
        {
            Assert.Contains("Audit.User.Created", item.Type);
            Assert.Contains(actorUserId, item.ActorUserId);
            Assert.Contains(email, item.Description);
        });
    }

    [Fact]
    public async Task ExportAuditLogs_ShouldReturnCsvForFilteredEntries()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var email = $"audit.export.{marker}@identityhub.com";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Audit@123",
            fullName = "Audit Export User"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var response = await _client.GetAsync($"/api/audit-logs/export?description={Uri.EscapeDataString(email)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();

        Assert.Contains("Id,ActorUserId,Type,Description,CreatedAt", csv);
        Assert.Contains(email, csv);
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Admin@123"
        });

        loginResponse.EnsureSuccessStatusCode();

        var payload = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(payload);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload!.Token);
    }

    private sealed class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }

    private sealed class UserListDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private sealed class PagedAuditLogResponseDto
    {
        public List<AuditLogItemDto> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class AuditLogItemDto
    {
        public Guid Id { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
