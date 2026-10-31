using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Abstractions;

public interface IAuditLog
{
    Task WriteAsync(AuditEntry entry, CancellationToken ct);
    Task<IReadOnlyList<AuditEntry>> GetForUserAsync(Guid userId, CancellationToken ct);
}
