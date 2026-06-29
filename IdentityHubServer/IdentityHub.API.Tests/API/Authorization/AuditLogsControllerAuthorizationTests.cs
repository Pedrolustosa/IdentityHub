using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuditLogsControllerAuthorizationTests
{
    [Theory]
    [InlineData("GetPaged")]
    [InlineData("GetById")]
    [InlineData("ExportCsv")]
    public void Action_ShouldInheritControllerPolicy(string methodName)
    {
        var method = typeof(AuditLogsController).GetMethod(methodName);

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
