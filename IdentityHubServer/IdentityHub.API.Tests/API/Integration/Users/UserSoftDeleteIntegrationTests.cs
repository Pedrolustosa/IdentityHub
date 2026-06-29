using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UserSoftDeleteIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserSoftDeleteIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task DeleteUser_ShouldSoftDeleteAndHideFromUsersEndpoints()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var email = $"soft.delete.{marker}@identityhub.com";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password = "Soft@123",
            fullName = "Soft Delete User"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var usersBeforeDeleteResponse = await _client.GetAsync("/api/users");
        usersBeforeDeleteResponse.EnsureSuccessStatusCode();

        var usersBeforeDelete = await usersBeforeDeleteResponse.Content.ReadFromJsonAsync<List<UserListDto>>();
        var createdUser = usersBeforeDelete?.SingleOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(createdUser);

        var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser!.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var usersAfterDeleteResponse = await _client.GetAsync("/api/users");
        usersAfterDeleteResponse.EnsureSuccessStatusCode();

        var usersAfterDelete = await usersAfterDeleteResponse.Content.ReadFromJsonAsync<List<UserListDto>>();
        Assert.DoesNotContain(usersAfterDelete ?? [], u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        var getByIdResponse = await _client.GetAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getByIdResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userInDatabase = await db.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Id == createdUser.Id);

        Assert.True(userInDatabase.IsDeleted);
        Assert.False(userInDatabase.IsActive);
        Assert.NotNull(userInDatabase.DeletedAt);
        Assert.False(string.IsNullOrWhiteSpace(userInDatabase.DeletedBy));
    }

    [Fact]
    public async Task DeleteUser_ShouldInvalidateExistingSessionsAndRefreshTokens()
    {
        await AuthenticateAsAdminAsync();

        var marker = Guid.NewGuid().ToString("N");
        var email = $"soft.sessions.{marker}@identityhub.com";
        var password = "Soft@123";

        var createResponse = await _client.PostAsJsonAsync("/api/users", new
        {
            email,
            password,
            fullName = "Soft Sessions User"
        });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var usersResponse = await _client.GetAsync("/api/users");
        usersResponse.EnsureSuccessStatusCode();

        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserListDto>>();
        var createdUser = users?.SingleOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(createdUser);

        var userLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, userLoginResponse.StatusCode);

        var userLogin = await userLoginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(userLogin);

        await AuthenticateAsAdminAsync();

        var deleteResponse = await _client.DeleteAsync($"/api/users/{createdUser!.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var loginAfterDeleteResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginAfterDeleteResponse.StatusCode);

        var refreshAfterDeleteResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = userLogin!.RefreshToken
        });

        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterDeleteResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sessions = await db.UserSessions
            .Where(x => x.UserId == createdUser.Id)
            .ToListAsync();

        var refreshTokens = await db.RefreshTokens
            .Where(x => x.UserId == createdUser.Id)
            .ToListAsync();

        Assert.NotEmpty(sessions);
        Assert.All(sessions, session => Assert.False(session.IsActive));

        Assert.NotEmpty(refreshTokens);
        Assert.All(refreshTokens, token => Assert.True(token.IsRevoked));
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

    private sealed class UserListDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private sealed class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
