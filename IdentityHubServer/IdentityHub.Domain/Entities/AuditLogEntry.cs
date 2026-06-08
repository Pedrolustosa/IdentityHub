namespace IdentityHub.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string ActorUserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
