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
    public async Task GetSessions_ShouldReturnOnlyCurrentUsersActiveSessions()
    {
        var firstAdminLogin = await LoginAsync("admin@identityhub.com", "Admin@123");
        var secondAdminLogin = await LoginAsync("admin@identityhub.com", "Admin@123");
        await LoginAsync("manager@identityhub.com", "Manager@123");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondAdminLogin.Token);

        var response = await _client.GetAsync("/api/auth/sessions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sessions = await response.Content.ReadFromJsonAsync<List<UserSessionDto>>();

        Assert.NotNull(sessions);
        var expectedSessionIds = new[]
        {
            ExtractSessionId(firstAdminLogin.Token),
            ExtractSessionId(secondAdminLogin.Token)
        };
        Assert.True(expectedSessionIds.All(id => sessions!.Any(x => x.Id == id)));
        Assert.Single(sessions!.Where(x => x.IsCurrent));
        Assert.Equal(ExtractSessionId(secondAdminLogin.Token), sessions.Single(x => x.IsCurrent).Id);
        Assert.DoesNotContain(sessions, x => x.Id == Guid.Empty);
        Assert.True(sessions.SequenceEqual(sessions.OrderByDescending(x => x.CreatedAt)));

        foreach (var expectedSessionId in expectedSessionIds)
        {
            var session = sessions.Single(x => x.Id == expectedSessionId);
            Assert.False(string.IsNullOrWhiteSpace(session.IpAddress));
            Assert.False(string.IsNullOrWhiteSpace(session.Browser));
            Assert.False(string.IsNullOrWhiteSpace(session.OperatingSystem));
        }
    }

    [Fact]
    public async Task RevokeSession_ShouldRemoveSpecificSessionAndInvalidateRefreshToken()
    {
        var currentLogin = await LoginAsync("admin@identityhub.com", "Admin@123");
        var otherLogin = await LoginAsync("admin@identityhub.com", "Admin@123");
        var currentSessionId = ExtractSessionId(currentLogin.Token);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", currentLogin.Token);

        var sessionsResponse = await _client.GetAsync("/api/auth/sessions");
        sessionsResponse.EnsureSuccessStatusCode();

        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<List<UserSessionDto>>();

        Assert.NotNull(sessions);

        var otherSessionId = ExtractSessionId(otherLogin.Token);
        var target = sessions!.Single(x => x.Id == otherSessionId);
        Assert.False(target.IsCurrent);

        var revokeResponse = await _client.DeleteAsync($"/api/auth/sessions/{target.Id}");

        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        var refreshed = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = otherLogin.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshed.StatusCode);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", otherLogin.Token);

        var revokedSessionMeResponse = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, revokedSessionMeResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", currentLogin.Token);

        var remainingSessionsResponse = await _client.GetAsync("/api/auth/sessions");
        remainingSessionsResponse.EnsureSuccessStatusCode();

        var remainingSessions = await remainingSessionsResponse.Content.ReadFromJsonAsync<List<UserSessionDto>>();

        Assert.NotNull(remainingSessions);
        Assert.DoesNotContain(remainingSessions!, x => x.Id == otherSessionId);
        Assert.Contains(remainingSessions!, x => x.Id == currentSessionId && x.IsCurrent);
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

    private static Guid ExtractSessionId(string jwt)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        var sid = token.Claims.Single(x => x.Type == "sid").Value;
        return Guid.Parse(sid);
    }

    private sealed class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class UserSessionDto
    {
        public Guid Id { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string Browser { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsCurrent { get; set; }
    }
}
