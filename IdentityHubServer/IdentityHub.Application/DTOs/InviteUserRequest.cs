namespace IdentityHub.Application.DTOs;

public sealed class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
}
