using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Common;

public static class Audit
{
    public const string AgentActor = "support-agent";

    public static AuditEntry Entry(User? actor, string action, Guid? targetUserId, string details) => new()
    {
        Id = Guid.NewGuid(),
        ActorUserId = actor?.Id,
        Actor = actor?.Login ?? AgentActor,
        Action = action,
        TargetUserId = targetUserId,
        Details = details,
        CreatedAt = DateTime.UtcNow
    };
}
