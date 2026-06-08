using IdentityHub.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace IdentityHub.Infrastructure.Security;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
        => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}

public sealed class ClientDeviceInfoProvider : IClientDeviceInfoProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientDeviceInfoProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public (string IpAddress, string Browser, string OperatingSystem) GetCurrent()
    {
        var context = _httpContextAccessor.HttpContext;

        var ipAddress = ResolveIpAddress(context);
        var userAgent = context?.Request.Headers["User-Agent"].ToString() ?? string.Empty;

        return (ipAddress, ResolveBrowser(userAgent), ResolveOperatingSystem(userAgent));
    }

    private static string ResolveIpAddress(HttpContext? context)
    {
        var forwardedFor = context?.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        var remote = context?.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remote) ? "Unknown" : remote;
    }

    private static string ResolveBrowser(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            return "Edge";
        if (userAgent.Contains("OPR/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
            return "Opera";
        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase))
            return "Chrome";
        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            return "Firefox";
        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase))
            return "Safari";

        return "Unknown";
    }

    private static string ResolveOperatingSystem(string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";

        if (userAgent.Contains("Windows NT", StringComparison.OrdinalIgnoreCase))
            return "Windows";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android";
        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iOS", StringComparison.OrdinalIgnoreCase))
            return "iOS";
        if (userAgent.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase))
            return "macOS";
        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
            return "Linux";

        return "Unknown";
    }
}
