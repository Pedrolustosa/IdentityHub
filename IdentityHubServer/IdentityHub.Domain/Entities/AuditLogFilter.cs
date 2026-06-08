namespace IdentityHub.Domain.Entities;

public class AuditLogFilter
{
    public string? Type { get; set; }
    public string? ActorUserId { get; set; }
    public string? Description { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}