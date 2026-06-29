using IdentityHub.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;
using Xunit;

namespace IdentityHub.API.Tests;

public sealed class CurrentUserContextAndDeviceInfoUnitTests
{
    [Fact]
    public void CurrentUserContext_UserId_ShouldReturnNull_WhenHttpContextIsMissing()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var sut = new CurrentUserContext(accessor);

        Assert.Null(sut.UserId);
    }

    [Fact]
    public void CurrentUserContext_UserId_ShouldReturnNull_WhenNameIdentifierClaimIsMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Email, "user@identityhub.com")],
            authenticationType: "test"));

        var context = new DefaultHttpContext { User = principal };
        var sut = new CurrentUserContext(new HttpContextAccessor { HttpContext = context });

        Assert.Null(sut.UserId);
    }

    [Fact]
    public void CurrentUserContext_UserId_ShouldReturnClaimValue_WhenPresent()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "user-123")],
            authenticationType: "test"));

        var context = new DefaultHttpContext { User = principal };
        var sut = new CurrentUserContext(new HttpContextAccessor { HttpContext = context });

        Assert.Equal("user-123", sut.UserId);
    }

    [Fact]
    public void CurrentUserContext_UserId_ShouldReturnNull_WhenUserPrincipalIsNull()
    {
        var context = new DefaultHttpContext
        {
            User = null!
        };

        var sut = new CurrentUserContext(new HttpContextAccessor { HttpContext = context });

        Assert.Null(sut.UserId);
    }

    [Fact]
    public void ClientDeviceInfoProvider_GetCurrent_ShouldUseForwardedForFirstValue_AndTrim()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = " 198.51.100.10 , 203.0.113.7 ";
        context.Request.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/126.0 Safari/537.36";

        var sut = new ClientDeviceInfoProvider(new HttpContextAccessor { HttpContext = context });

        var result = sut.GetCurrent();

        Assert.Equal("198.51.100.10", result.IpAddress);
        Assert.Equal("Chrome", result.Browser);
        Assert.Equal("Windows", result.OperatingSystem);
    }

    [Fact]
    public void ClientDeviceInfoProvider_GetCurrent_ShouldFallbackToRemoteIp_WhenForwardedForIsMissing()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.50");
        context.Request.Headers["User-Agent"] = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Safari/605.1.15";

        var sut = new ClientDeviceInfoProvider(new HttpContextAccessor { HttpContext = context });

        var result = sut.GetCurrent();

        Assert.Equal("203.0.113.50", result.IpAddress);
        Assert.Equal("Safari", result.Browser);
        Assert.Equal("macOS", result.OperatingSystem);
    }

    [Fact]
    public void ClientDeviceInfoProvider_GetCurrent_ShouldReturnUnknownDefaults_WhenContextOrAgentIsMissing()
    {
        var sut = new ClientDeviceInfoProvider(new HttpContextAccessor { HttpContext = null });

        var result = sut.GetCurrent();

        Assert.Equal("Unknown", result.IpAddress);
        Assert.Equal("Unknown", result.Browser);
        Assert.Equal("Unknown", result.OperatingSystem);
    }

    [Fact]
    public void ClientDeviceInfoProvider_GetCurrent_ShouldReturnUnknownIp_WhenForwardedForHasNoValidIp_AndRemoteIsMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = " ,  ,   ";
        context.Request.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0)";

        var sut = new ClientDeviceInfoProvider(new HttpContextAccessor { HttpContext = context });

        var result = sut.GetCurrent();

        Assert.Equal("Unknown", result.IpAddress);
        Assert.Equal("Windows", result.OperatingSystem);
    }

    [Theory]
    [InlineData("Mozilla/5.0 Edg/125.0", "Edge")]
    [InlineData("Mozilla/5.0 OPR/109.0", "Opera")]
    [InlineData("Mozilla/5.0 Opera/9.80", "Opera")]
    [InlineData("Mozilla/5.0 Firefox/127.0", "Firefox")]
    [InlineData("SomeRandomAgent", "Unknown")]
    public void ClientDeviceInfoProvider_GetCurrent_ShouldResolveBrowser_FromUserAgent(string userAgent, string expectedBrowser)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Headers["User-Agent"] = userAgent;

        var sut = new ClientDeviceInfoProvider(new HttpContextAccessor { HttpContext = context });

        var result = sut.GetCurrent();

        Assert.Equal(expectedBrowser, result.Browser);
    }

    [Theory]
    [InlineData("Mozilla/5.0 (Linux; Android 14)", "Android")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)", "iOS")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64)", "Linux")]
    [InlineData("CustomAgent", "Unknown")]
    public void ClientDeviceInfoProvider_GetCurrent_ShouldResolveOperatingSystem_FromUserAgent(string userAgent, string expectedOs)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Headers["User-Agent"] = userAgent;

        var sut = new ClientDeviceInfoProvider(new HttpContextAccessor { HttpContext = context });

        var result = sut.GetCurrent();

        Assert.Equal(expectedOs, result.OperatingSystem);
    }
}
