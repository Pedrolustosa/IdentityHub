using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuditLogsControllerAuthorizationTests
{
    [Fact]
    public void GetPaged_ShouldDeclareExpectedPolicy()
    {
        var method = typeof(AuditLogsController).GetMethod("GetPaged");

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.Null(authorize);

        var controllerAuthorize = typeof(AuditLogsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(controllerAuthorize);
        Assert.Equal("Audit.View", controllerAuthorize!.Policy);
    }
}
