using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class RolesControllerAuthorizationTests
{
    [Theory]
    [InlineData("GetAll", "Roles.View")]
    [InlineData("GetById", "Roles.View")]
    [InlineData("Create", "Roles.Create")]
    [InlineData("Update", "Roles.Update")]
    [InlineData("Delete", "Roles.Delete")]
    [InlineData("GetPermissionCatalog", "Roles.Permissions.View")]
    [InlineData("GetPermissions", "Roles.Permissions.View")]
    [InlineData("UpdatePermissions", "Roles.Permissions.Update")]
    public void Action_ShouldDeclareExpectedPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(RolesController).GetMethod(methodName);

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }
}
