using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthControllerAuthorizationTests
{
    [Theory]
    [InlineData("ChangePassword")]
    [InlineData("GetRecentSessions")]
    public void Action_ShouldRequireAuthenticatedUser(string methodName)
    {
        var method = typeof(AuthController).GetMethod(methodName);

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.True(string.IsNullOrWhiteSpace(authorize!.Policy));
    }

    [Fact]
    public void RevokeUserSessions_ShouldRequireSessionsRevokePolicy()
    {
        var method = typeof(AuthController).GetMethod("RevokeUserSessions");

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal("Sessions.Revoke", authorize!.Policy);
    }
}
