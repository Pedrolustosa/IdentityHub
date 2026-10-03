namespace IdentityHub.Application.DTOs;

public sealed class AdminSessionResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrent { get; set; }
}
