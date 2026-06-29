using IdentityHub.API.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class PermissionMiddlewareUnitTests
{
    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenNoEndpoint()
    {
        var wasCalled = false;
        var middleware = new PermissionMiddleware(_ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(wasCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenEndpointHasNoPolicies()
    {
        var wasCalled = false;
        var middleware = new PermissionMiddleware(_ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute()),
            "test");
        context.SetEndpoint(endpoint);

        await middleware.InvokeAsync(context);

        Assert.True(wasCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_WhenUnauthenticatedAndPolicyExists()
    {
        var middleware = new PermissionMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute { Policy = "Users.View" }),
            "test");
        context.SetEndpoint(endpoint);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn403_WhenAuthenticatedWithoutPermission()
    {
        var middleware = new PermissionMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var identity = new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim("permission", "Roles.View")
        ],
        authenticationType: "test");

        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute { Policy = "Users.View" }),
            "test");
        context.SetEndpoint(endpoint);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenAuthenticatedWithPermission()
    {
        var wasCalled = false;
        var middleware = new PermissionMiddleware(_ =>
        {
            wasCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();

        var identity = new System.Security.Claims.ClaimsIdentity(
        [
            new System.Security.Claims.Claim("permission", "Users.View")
        ],
        authenticationType: "test");

        context.User = new System.Security.Claims.ClaimsPrincipal(identity);

        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute { Policy = "Users.View" }),
            "test");
        context.SetEndpoint(endpoint);

        await middleware.InvokeAsync(context);

        Assert.True(wasCalled);
    }

    [Fact]
    public void UsePermissionMiddleware_ShouldReturnApplicationBuilder()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        var result = app.UsePermissionMiddleware();

        Assert.NotNull(result);
        Assert.Same(app, result);
    }
}
