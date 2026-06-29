using IdentityHub.API.Controllers;
using System.Reflection;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class UserInvitesControllerAuthorizationTests
{
    [Theory]
    [InlineData("GetPaged", "UserInvites.View")]
    [InlineData("ResendInvite", "UserInvites.Resend")]
    [InlineData("CancelInvite", "UserInvites.Cancel")]
    public void PublicActions_HasCorrectPolicy(string actionName, string expectedPolicy)
    {
        // Arrange
        var action = typeof(UserInvitesController)
            .GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(action);

        // Act
        var attribute = action!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(expectedPolicy, attribute.Policy);
    }
}
