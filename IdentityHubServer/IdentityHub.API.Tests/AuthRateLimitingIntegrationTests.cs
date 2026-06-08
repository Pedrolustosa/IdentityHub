using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthRateLimitingIntegrationTests
{
    [Fact]
    public async Task Login_ShouldReturn429_WhenRateLimitIsExceeded()
    {
        await using var factory = CreateFactoryWithStrictRateLimit();
        using var client = CreateClient(factory);

        var first = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "wrong-password"
        });
        var second = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "wrong-password"
        });
        var third = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@identityhub.com",
            password = "wrong-password"
        });

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn429_WhenRateLimitIsExceeded()
    {
        await using var factory = CreateFactoryWithStrictRateLimit();
        using var client = CreateClient(factory);

        var first = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "admin@identityhub.com"
        });
        var second = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "admin@identityhub.com"
        });
        var third = await client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = "admin@identityhub.com"
        });

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_ShouldReturn429_WhenRateLimitIsExceeded()
    {
        await using var factory = CreateFactoryWithStrictRateLimit();
        using var client = CreateClient(factory);

        var first = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new
        {
            email = "admin@identityhub.com"
        });
        var second = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new
        {
            email = "admin@identityhub.com"
        });
        var third = await client.PostAsJsonAsync("/api/auth/resend-confirmation", new
        {
            email = "admin@identityhub.com"
        });

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    private static TestWebApplicationFactory CreateFactoryWithStrictRateLimit()
    {
        return new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:Login:PermitLimit"] = "2",
            ["RateLimiting:Auth:Login:WindowSeconds"] = "120",
            ["RateLimiting:Auth:ForgotPassword:PermitLimit"] = "2",
            ["RateLimiting:Auth:ForgotPassword:WindowSeconds"] = "120",
            ["RateLimiting:Auth:ResendConfirmation:PermitLimit"] = "2",
            ["RateLimiting:Auth:ResendConfirmation:WindowSeconds"] = "120"
        });
    }

    private static HttpClient CreateClient(TestWebApplicationFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}
