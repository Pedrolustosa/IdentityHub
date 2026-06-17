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

public sealed class SecurityHardeningIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityHardeningIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task ReusingRevokedRefreshToken_ShouldRaiseSecurityEventAndRevokeSession()
    {
        var login = await LoginAsync("manager@identityhub.com", "Manager@123");
        var sessionId = ExtractSessionId(login.Token);

        var firstRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var reuseEventExists = await db.SecurityEvents.AnyAsync(e =>
            e.Type == SecurityAlertEventTypes.RefreshTokenReuse &&
            e.Severity == SecurityEventSeverity.Critical &&
            e.Description.Contains(sessionId.ToString()));

        Assert.True(reuseEventExists);

        var session = await db.UserSessions.FirstAsync(s => s.Id == sessionId);
        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task ChangePassword_ShouldWriteAuditEntry()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var email = $"audit.pwd.{marker}@identityhub.com";
        const string currentPassword = "Initial@123";
        const string newPassword = "Updated@123";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = currentPassword,
            fullName = "Audit Pwd User"
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        using var userClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var userLogin = await LoginAsync(userClient, email, currentPassword);
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userLogin.Token);

        var change = await userClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword,
            newPassword
        });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditExists = await db.AuditLogEntries.AnyAsync(e =>
            e.Type == "Audit.User.PasswordChanged" &&
            e.Description.Contains(email));

        Assert.True(auditExists);
    }

    [Fact]
    public async Task RevokeSession_ShouldWriteAuditEntry()
    {
        var current = await LoginAsync("admin@identityhub.com", "Admin@123");
        var other = await LoginAsync("admin@identityhub.com", "Admin@123");
        var otherSessionId = ExtractSessionId(other.Token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", current.Token);

        var revoke = await _client.DeleteAsync($"/api/auth/sessions/{otherSessionId}");
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var auditExists = await db.AuditLogEntries.AnyAsync(e =>
            e.Type == "Audit.Session.Revoked" &&
            e.Description.Contains(otherSessionId.ToString()));

        Assert.True(auditExists);
    }

    [Fact]
    public async Task UpdateSecurityAlertStatus_ShouldPersistNewStatus()
    {
        await AuthenticateAsAdminAsync();

        await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "Wrong@123"
        });

        Guid alertId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            alertId = await db.SecurityEvents
                .Where(e => e.Type == SecurityAlertEventTypes.SuspiciousLogin)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Id)
                .FirstAsync();
        }

        var update = await _client.PutAsJsonAsync($"/api/security-alerts/{alertId}/status", new
        {
            status = SecurityEventStatus.Resolved
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var status = await db.SecurityEvents.Where(e => e.Id == alertId).Select(e => e.Status).FirstAsync();
            Assert.Equal(SecurityEventStatus.Resolved, status);
        }
    }

    [Fact]
    public async Task UpdateSecurityAlertStatus_WithInvalidStatus_ShouldReturnBadRequest()
    {
        await AuthenticateAsAdminAsync();

        var response = await _client.PutAsJsonAsync($"/api/security-alerts/{Guid.NewGuid()}/status", new
        {
            status = "NotAValidStatus"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task AuthenticateAsAdminAsync()
    {
        var login = await LoginAsync("admin@identityhub.com", "Admin@123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
    }

    private Task<AuthResponseDto> LoginAsync(string email, string password) => LoginAsync(_client, email, password);

    private static async Task<AuthResponseDto> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
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
}
