namespace MlhSupport.Domain.Entities;

public class AuditEntry
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? TargetUserId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
