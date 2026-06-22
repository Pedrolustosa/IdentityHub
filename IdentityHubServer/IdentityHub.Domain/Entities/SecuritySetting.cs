namespace IdentityHub.Domain.Entities;

public class SecuritySetting
{
    public Guid Id { get; set; }
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 7;
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockDurationMinutes { get; set; } = 15;
    public bool RequireEmailConfirmation { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
