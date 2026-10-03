using IdentityHub.Application.DTOs;
using IdentityHub.Application.Interfaces;

namespace IdentityHub.Application.Common.Security;

public static class SecuritySettingsExtensions
{
    public static async Task<SecuritySettingsResponse> GetOrDefaultAsync(
        this ISecuritySettingsService service,
        CancellationToken cancellationToken = default)
    {
        var result = await service.GetSettingsAsync(cancellationToken);
        if (result.IsSuccess && result.Value is not null)
            return result.Value;

        return SecuritySettingsDefaults.Create();
    }
}
