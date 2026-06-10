namespace IdentityHub.Domain.Entities;

public sealed class SecurityAlertFilter
{
    public string? Type { get; set; }
    public string? UserId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}