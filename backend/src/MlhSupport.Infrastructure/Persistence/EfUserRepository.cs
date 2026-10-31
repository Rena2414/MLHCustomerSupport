using Microsoft.EntityFrameworkCore;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Infrastructure.Persistence;

public class EfUserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByLoginAsync(string login, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.Login == login, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(UserRole? role, CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => role == null || u.Role == role).ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var idList = ids.Distinct().ToList();
        return idList.Count == 0 ? [] : await db.Users.AsNoTracking().Where(u => idList.Contains(u.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        if (db.Entry(user).State == EntityState.Detached)
        {
            db.Users.Update(user);
        }
        await db.SaveChangesAsync(ct);
    }
}
