using IdentityHub.API.Controllers;
using System.Reflection;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class SecuritySettingsControllerAuthorizationTests
{
    [Theory]
    [InlineData("Get", "SecuritySettings.View")]
    [InlineData("Update", "SecuritySettings.Update")]
    public void PublicActions_HasCorrectPolicy(string actionName, string expectedPolicy)
    {
        // Arrange
        var action = typeof(SecuritySettingsController)
            .GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(action);

        // Act
        var attribute = action!.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal(expectedPolicy, attribute.Policy);
    }
}
