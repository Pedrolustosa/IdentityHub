using IdentityHub.API.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class GlobalExceptionMiddlewareUnitTests
{
    [Fact]
    public async Task InvokeAsync_ShouldSet499_WhenOperationCanceledExceptionIsThrown()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new OperationCanceledException(),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(499, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorizedPayload_WhenUnauthorizedAccessExceptionIsThrown()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new UnauthorizedAccessException("denied"),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var payload = await ReadJsonPayloadAsync(context);
        Assert.Equal("Unauthorized", payload.GetProperty("error").GetString());
        Assert.Equal("denied", payload.GetProperty("message").GetString());
        Assert.Equal("trace-test", payload.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnBadRequestPayload_WhenInvalidOperationExceptionIsThrown()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new InvalidOperationException("invalid state"),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var payload = await ReadJsonPayloadAsync(context);
        Assert.Equal("BadRequest", payload.GetProperty("error").GetString());
        Assert.Equal("invalid state", payload.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnGeneric500Payload_WhenUnhandledExceptionIsThrown()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new Exception("secret details"),
            NullLogger<GlobalExceptionMiddleware>.Instance);

        var context = CreateHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var payload = await ReadJsonPayloadAsync(context);
        Assert.Equal("InternalServerError", payload.GetProperty("error").GetString());
        Assert.Equal("An unexpected error occurred", payload.GetProperty("message").GetString());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-test";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadJsonPayloadAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }
}
