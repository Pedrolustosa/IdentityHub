using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class SessionsControllerAuthorizationTests
{
    [Fact]
    public void GetPaged_ShouldInheritControllerPolicy()
    {
        var method = typeof(SessionsController).GetMethod(nameof(SessionsController.GetPaged));

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.Null(authorize);

        var controllerAuthorize = typeof(SessionsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("Sessions.View", controllerAuthorize!.Policy);
    }

    [Fact]
    public void Revoke_ShouldRequireSessionsRevokePolicy()
    {
        var method = typeof(SessionsController).GetMethod(nameof(SessionsController.Revoke));

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal("Sessions.Revoke", authorize!.Policy);
    }
}
