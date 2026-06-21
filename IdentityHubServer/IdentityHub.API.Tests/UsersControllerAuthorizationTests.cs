using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UsersControllerAuthorizationTests
{
    [Theory]
    [InlineData("GetAll", "Users.View")]
    [InlineData("GetById", "Users.View")]
    [InlineData("Create", "Users.Create")]
    [InlineData("Invite", "Users.Create")]
    [InlineData("Update", "Users.Update")]
    [InlineData("Delete", "Users.Delete")]
    [InlineData("UpdateRoles", "Users.Roles.Update")]
    [InlineData("GetSessionsByUser", "Users.View")]
    [InlineData("RevokeUserSession", "Users.Update")]
    [InlineData("GetAuditLogsByUser", "Audit.View")]
    public void Action_ShouldDeclareExpectedPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(UsersController).GetMethod(methodName);

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }
}
