using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthFlowIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthFlowIntegrationTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Login_ShouldReturnTokenAndRefreshToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Admin@123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
    }

    [Fact]
    public async Task Logout_WithDifferentUserRefreshToken_ShouldReturnForbidden()
    {
        var adminLogin = await LoginAsync("admin@identityhub.com", "Admin@123");
        var managerLogin = await LoginAsync("manager@identityhub.com", "Manager@123");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", managerLogin.Token);

        var response = await _client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = adminLogin.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        var login = await LoginAsync("admin@identityhub.com", "Admin@123");

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.Token));
        Assert.False(string.IsNullOrWhiteSpace(refreshed.RefreshToken));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithRevokedRefreshToken_ShouldReturnUnauthorized()
    {
        var login = await LoginAsync("admin@identityhub.com", "Admin@123");

        var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        var secondRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = login.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidTokenFormat_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email = "admin@identityhub.com",
            token = "not-base64-url-token",
            newPassword = "Admin@123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidTokenFormat_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync(
            "/api/auth/confirm-email?email=admin%40identityhub.com&token=not-base64-url-token");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmEmail_WithNonExistingUser_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync(
            "/api/auth/confirm-email?email=ghost%40identityhub.com&token=any-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithNonExistingUser_ShouldReturnNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            email = "ghost@identityhub.com",
            token = "validlen",
            newPassword = "Admin@123"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "not-an-email",
            password = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(payload);

        return payload!;
    }

    private sealed class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
