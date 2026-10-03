namespace IdentityHub.Domain.Entities;

public sealed class SessionFilter
{
    public bool ActiveOnly { get; set; } = true;
    public string? UserId { get; set; }
    public string? Search { get; set; }
}
