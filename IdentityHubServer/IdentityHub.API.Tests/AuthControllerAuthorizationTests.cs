using IdentityHub.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class AuthControllerAuthorizationTests
{
    [Fact]
    public void ChangePassword_ShouldRequireAuthenticatedUser()
    {
        var method = typeof(AuthController).GetMethod("ChangePassword");

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorize);
        Assert.True(string.IsNullOrWhiteSpace(authorize!.Policy));
    }
}
