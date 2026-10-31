using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Abstractions;

public interface IFamilyRepository
{
    Task<IReadOnlyList<Guid>> GetChildIdsAsync(Guid parentId, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetParentIdsAsync(Guid childId, CancellationToken ct);
    Task<bool> ExistsAsync(Guid parentId, Guid childId, CancellationToken ct);
    Task AddAsync(ParentChild link, CancellationToken ct);
    Task<bool> RemoveAsync(Guid parentId, Guid childId, CancellationToken ct);
}
