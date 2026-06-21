using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityHub.Domain.Constants;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class SecurityAlertsEndpointIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAlertsEndpointIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetSecurityAlerts_ShouldReturnPagedAlerts()
    {
        await AuthenticateAsAdminAsync();

        await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Wrong@123"
        });

        var response = await _client.GetAsync("/api/security-alerts?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedSecurityAlertResponseDto>();

        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(10, payload.PageSize);
        Assert.True(payload.TotalCount >= 1);
        Assert.Contains(payload.Items, x => x.Type == SecurityAlertEventTypes.SuspiciousLogin);
    }

    [Fact]
    public async Task GetSecurityAlerts_ShouldApplyTypeUserAndPeriodFilters()
    {
        await AuthenticateAsAdminAsync();

        await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Wrong@123"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var adminId = await db.Users
            .Where(u => u.Email == "admin@identityhub.com")
            .Select(u => u.Id)
            .SingleAsync();

        var fromDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var response = await _client.GetAsync(
            $"/api/security-alerts?page=1&pageSize=10&type={Uri.EscapeDataString(SecurityAlertEventTypes.SuspiciousLogin)}&userId={Uri.EscapeDataString(adminId)}&fromDate={fromDate}&toDate={toDate}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedSecurityAlertResponseDto>();

        Assert.NotNull(payload);
        Assert.NotEmpty(payload!.Items);
        Assert.All(payload.Items, item =>
        {
            Assert.Contains(SecurityAlertEventTypes.SuspiciousLogin, item.Type);
            Assert.Contains(adminId, item.UserId);
        });
    }

    [Fact]
    public async Task GetSecurityAlertById_ShouldReturnAlertDetails()
    {
        await AuthenticateAsAdminAsync();

        await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Wrong@123"
        });

        var listResponse = await _client.GetAsync("/api/security-alerts?page=1&pageSize=1");
        listResponse.EnsureSuccessStatusCode();

        var listPayload = await listResponse.Content.ReadFromJsonAsync<PagedSecurityAlertResponseDto>();
        Assert.NotNull(listPayload);
        Assert.NotEmpty(listPayload!.Items);

        var item = listPayload.Items.First();

        var detailResponse = await _client.GetAsync($"/api/security-alerts/{item.Id}");

        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var detail = await detailResponse.Content.ReadFromJsonAsync<SecurityAlertItemDto>();

        Assert.NotNull(detail);
        Assert.Equal(item.Id, detail!.Id);
        Assert.Equal(item.Type, detail.Type);
        Assert.Equal(item.UserId, detail.UserId);
        Assert.False(string.IsNullOrWhiteSpace(detail.Status));
        Assert.False(string.IsNullOrWhiteSpace(detail.Severity));
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

    private sealed class PagedSecurityAlertResponseDto
    {
        public List<SecurityAlertItemDto> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class SecurityAlertItemDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}