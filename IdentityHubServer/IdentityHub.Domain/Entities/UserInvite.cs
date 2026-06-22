namespace IdentityHub.Domain.Entities;

public class UserInvite
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Accepted, Expired, Canceled
    public DateTime SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public string Roles { get; set; } = string.Empty; // Comma-separated roles
    public bool IsActive { get; set; } = true;
}
