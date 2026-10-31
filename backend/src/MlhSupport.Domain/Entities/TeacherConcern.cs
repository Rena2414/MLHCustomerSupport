using MlhSupport.Domain.Enums;

namespace MlhSupport.Domain.Entities;

public class TeacherConcern
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid? ConversationId { get; set; }
    public string Text { get; set; } = string.Empty;
    public ConcernCategory Category { get; set; }
    public ConcernSeverity Severity { get; set; }
    public string Summary { get; set; } = string.Empty;
    public ConcernStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
