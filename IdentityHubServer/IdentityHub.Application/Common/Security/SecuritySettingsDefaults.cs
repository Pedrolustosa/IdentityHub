using IdentityHub.Application.DTOs;

namespace IdentityHub.Application.Common.Security;

public static class SecuritySettingsDefaults
{
    public const int AccessTokenMinutes = 30;
    public const int RefreshTokenDays = 7;
    public const int MaxLoginAttempts = 5;
    public const int LockDurationMinutes = 15;
    public const bool RequireEmailConfirmation = true;

    public static SecuritySettingsResponse Create() => new()
    {
        AccessTokenMinutes = AccessTokenMinutes,
        RefreshTokenDays = RefreshTokenDays,
        MaxLoginAttempts = MaxLoginAttempts,
        LockDurationMinutes = LockDurationMinutes,
        RequireEmailConfirmation = RequireEmailConfirmation
    };
}
