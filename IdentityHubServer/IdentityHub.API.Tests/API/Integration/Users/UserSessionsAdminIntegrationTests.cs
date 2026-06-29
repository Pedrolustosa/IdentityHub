using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UserSessionsAdminIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public UserSessionsAdminIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetUserSessions_AsAdmin_ShouldReturnTargetUserSessions()
    {
        var managerLoginA = await LoginAsync("manager@identityhub.com", "Manager@123");
        var managerLoginB = await LoginAsync("manager@identityhub.com", "Manager@123");
        var managerSessionA = ExtractSessionId(managerLoginA.Token);
        var managerSessionB = ExtractSessionId(managerLoginB.Token);

        var adminLogin = await LoginAsync("admin@identityhub.com", "Admin@123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.Token);

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListDto>>();
        Assert.NotNull(users);

        var managerId = users!
            .First(u => string.Equals(u.Email, "manager@identityhub.com", StringComparison.OrdinalIgnoreCase))
            .Id;

        var sessionsResponse = await _client.GetAsync($"/api/users/{managerId}/sessions?take=10");

        Assert.Equal(HttpStatusCode.OK, sessionsResponse.StatusCode);

        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<List<UserSessionDto>>();

        Assert.NotNull(sessions);
        Assert.Contains(sessions!, x => x.Id == managerSessionA);
        Assert.Contains(sessions!, x => x.Id == managerSessionB);
        Assert.All(sessions!, session => Assert.False(session.IsCurrent));
    }

    [Fact]
    public async Task RevokeUserSession_AsAdmin_ShouldDeactivateTargetSession()
    {
        var managerLoginA = await LoginAsync("manager@identityhub.com", "Manager@123");
        var managerLoginB = await LoginAsync("manager@identityhub.com", "Manager@123");
        var managerSessionA = ExtractSessionId(managerLoginA.Token);

        var adminLogin = await LoginAsync("admin@identityhub.com", "Admin@123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin.Token);

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListDto>>();
        Assert.NotNull(users);

        var managerId = users!
            .First(u => string.Equals(u.Email, "manager@identityhub.com", StringComparison.OrdinalIgnoreCase))
            .Id;

        var revokeResponse = await _client.DeleteAsync($"/api/users/{managerId}/sessions/{managerSessionA}");
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        var sessionsResponse = await _client.GetAsync($"/api/users/{managerId}/sessions?take=10");
        sessionsResponse.EnsureSuccessStatusCode();

        var sessions = await sessionsResponse.Content.ReadFromJsonAsync<List<UserSessionDto>>();
        Assert.NotNull(sessions);

        var revoked = sessions!.Single(x => x.Id == managerSessionA);
        Assert.False(revoked.IsActive);
        Assert.NotNull(revoked.RevokedAt);

        using var revokedClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        revokedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerLoginA.Token);

        var meResponse = await revokedClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        using var activeClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        activeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerLoginB.Token);

        var activeMeResponse = await activeClient.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, activeMeResponse.StatusCode);
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
    }

    private sealed class UserListDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class UserSessionDto
    {
        public Guid Id { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
