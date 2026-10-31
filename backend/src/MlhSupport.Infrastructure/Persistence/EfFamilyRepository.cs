using Microsoft.EntityFrameworkCore;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class EfFamilyRepository(AppDbContext db) : IFamilyRepository
{
    public async Task<IReadOnlyList<Guid>> GetChildIdsAsync(Guid parentId, CancellationToken ct) =>
        await db.ParentChildren.Where(p => p.ParentId == parentId).Select(p => p.ChildId).ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetParentIdsAsync(Guid childId, CancellationToken ct) =>
        await db.ParentChildren.Where(p => p.ChildId == childId).Select(p => p.ParentId).ToListAsync(ct);

    public Task<bool> ExistsAsync(Guid parentId, Guid childId, CancellationToken ct) =>
        db.ParentChildren.AnyAsync(p => p.ParentId == parentId && p.ChildId == childId, ct);

    public async Task AddAsync(ParentChild link, CancellationToken ct)
    {
        db.ParentChildren.Add(link);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> RemoveAsync(Guid parentId, Guid childId, CancellationToken ct) =>
        await db.ParentChildren.Where(p => p.ParentId == parentId && p.ChildId == childId).ExecuteDeleteAsync(ct) > 0;
}
