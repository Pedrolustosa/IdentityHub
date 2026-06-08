namespace IdentityHub.Application.DTOs;

public sealed class UserSessionResponse
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCurrent { get; set; }
}