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

public sealed class SecurityAlertsIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAlertsIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task LoginWithInvalidPassword_ShouldCreateSuspiciousLoginSecurityAlert()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Wrong@123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var adminId = await db.Users
            .Where(u => u.Email == "admin@identityhub.com")
            .Select(u => u.Id)
            .SingleAsync();

        var alertExists = await db.SecurityEvents.AnyAsync(e =>
            e.UserId == adminId &&
            e.Type == SecurityAlertEventTypes.SuspiciousLogin);

        Assert.True(alertExists);
    }

    [Fact]
    public async Task ChangePassword_ShouldCreateCriticalActionSecurityAlert()
    {
        await AuthenticateAsAdminAsync(_client);

        var marker = Guid.NewGuid().ToString("N");
        var email = $"security.alert.{marker}@identityhub.com";
        const string currentPassword = "Initial@123";
        const string newPassword = "Updated@123";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = currentPassword,
            fullName = "Security Alert User"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        using var userClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var loginResponse = await userClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = currentPassword
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(loginPayload);

        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginPayload!.Token);

        var changeResponse = await userClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword,
            newPassword
        });

        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = await db.Users
            .Where(u => u.Email == email)
            .Select(u => u.Id)
            .SingleAsync();

        var alertExists = await db.SecurityEvents.AnyAsync(e =>
            e.UserId == userId &&
            e.Type == SecurityAlertEventTypes.CriticalAction &&
            e.Description.Contains("Password changed"));

        Assert.True(alertExists);
    }

    private static async Task AuthenticateAsAdminAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Admin@123"
        });

        loginResponse.EnsureSuccessStatusCode();

        var payload = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(payload);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", payload!.Token);
    }

    private sealed class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
    }
}