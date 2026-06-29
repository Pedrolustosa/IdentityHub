using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class RoleClaimsControllerAuthorizationTests
{
    [Theory]
    [InlineData("GetPermissions", "Roles.Permissions.View")]
    [InlineData("AddPermission", "Roles.Permissions.Update")]
    [InlineData("RemovePermission", "Roles.Permissions.Update")]
    [InlineData("ReplacePermissions", "Roles.Permissions.Update")]
    public void Action_ShouldDeclareExpectedPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(RoleClaimsController).GetMethod(methodName);

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }
}