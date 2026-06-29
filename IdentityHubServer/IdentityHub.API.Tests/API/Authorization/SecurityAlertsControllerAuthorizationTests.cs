using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class SecurityAlertsControllerAuthorizationTests
{
    [Theory]
    [InlineData("GetPaged")]
    [InlineData("GetById")]
    public void ViewActions_ShouldInheritControllerPolicy(string methodName)
    {
        var method = typeof(SecurityAlertsController).GetMethod(methodName);

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.Null(authorize);

        var controllerAuthorize = typeof(SecurityAlertsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("SecurityEvents.View", controllerAuthorize!.Policy);
    }

    [Fact]
    public void UpdateStatus_ShouldRequireManagePolicy()
    {
        var method = typeof(SecurityAlertsController).GetMethod("UpdateStatus");

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal("SecurityEvents.Manage", authorize!.Policy);
    }
}