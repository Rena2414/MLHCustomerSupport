using Microsoft.EntityFrameworkCore;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class EfClassRepository(AppDbContext db) : IClassRepository
{
    public async Task<IReadOnlyList<SchoolClass>> GetAllAsync(CancellationToken ct) =>
        await db.Classes.AsNoTracking().Include(c => c.Enrollments).ToListAsync(ct);

    public async Task<IReadOnlyList<SchoolClass>> GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
        await db.Classes.AsNoTracking().Include(c => c.Enrollments).Where(c => c.TeacherId == teacherId).ToListAsync(ct);

    public async Task<IReadOnlyList<SchoolClass>> GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        await db.Classes.AsNoTracking().Include(c => c.Enrollments)
            .Where(c => c.Enrollments.Any(e => e.StudentId == studentId))
            .ToListAsync(ct);

    public Task<SchoolClass?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Classes.AsNoTracking().Include(c => c.Enrollments).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(SchoolClass schoolClass, CancellationToken ct)
    {
        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync(ct);
        db.Entry(schoolClass).State = EntityState.Detached;
    }

    public async Task AddEnrollmentAsync(ClassEnrollment enrollment, CancellationToken ct)
    {
        db.ClassEnrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);
        db.Entry(enrollment).State = EntityState.Detached;
    }

    public async Task<bool> RemoveEnrollmentAsync(Guid classId, Guid studentId, CancellationToken ct) =>
        await db.ClassEnrollments.Where(e => e.ClassId == classId && e.StudentId == studentId).ExecuteDeleteAsync(ct) > 0;
}
