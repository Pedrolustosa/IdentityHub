namespace IdentityHub.Application.DTOs;

public sealed class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; } = true;
    public IList<string> Roles { get; set; } = [];
}
