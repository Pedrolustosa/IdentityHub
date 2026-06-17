namespace IdentityHub.Application.DTOs;

public sealed class UserSessionResponse
{
    public Guid Id { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public bool IsCurrent { get; set; }
}