using Microsoft.EntityFrameworkCore;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class EfConcernRepository(AppDbContext db) : IConcernRepository
{
    public async Task<IReadOnlyList<TeacherConcern>> GetAllAsync(CancellationToken ct) =>
        await db.TeacherConcerns.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<TeacherConcern>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
        await db.TeacherConcerns.AsNoTracking().Where(c => c.TeacherId == teacherId).ToListAsync(ct);

    public async Task<IReadOnlyList<TeacherConcern>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        await db.TeacherConcerns.AsNoTracking().Where(c => c.StudentId == studentId).ToListAsync(ct);

    public Task<TeacherConcern?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.TeacherConcerns.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(TeacherConcern concern, CancellationToken ct)
    {
        db.TeacherConcerns.Add(concern);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TeacherConcern concern, CancellationToken ct)
    {
        if (db.Entry(concern).State == EntityState.Detached)
        {
            db.TeacherConcerns.Update(concern);
        }
        await db.SaveChangesAsync(ct);
    }
}

public class EfAuditLog(AppDbContext db) : IAuditLog
{
    public async Task WriteAsync(AuditEntry entry, CancellationToken ct)
    {
        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetForUserAsync(Guid userId, CancellationToken ct) =>
        await db.AuditEntries.AsNoTracking()
            .Where(a => a.TargetUserId == userId || a.ActorUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
}
