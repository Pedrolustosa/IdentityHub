namespace IdentityHub.Domain.Entities;

public sealed class UserSessionListItem
{
    public UserSession Session { get; set; } = null!;
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
