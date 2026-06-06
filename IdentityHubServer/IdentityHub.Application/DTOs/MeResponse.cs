namespace IdentityHub.Application.DTOs;

public sealed class MeResponse
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public IList<string> Roles { get; set; } = [];
    public IList<string> Permissions { get; set; } = [];
}
