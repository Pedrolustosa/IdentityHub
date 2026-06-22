namespace IdentityHub.Application.DTOs;

public sealed class SecuritySettingsResponse
{
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
    public int MaxLoginAttempts { get; set; }
    public int LockDurationMinutes { get; set; }
    public bool RequireEmailConfirmation { get; set; }
}

public sealed class UpdateSecuritySettingsRequest
{
    public int AccessTokenMinutes { get; set; }
    public int RefreshTokenDays { get; set; }
    public int MaxLoginAttempts { get; set; }
    public int LockDurationMinutes { get; set; }
    public bool RequireEmailConfirmation { get; set; }
}
