using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Abstractions;

public interface IClassRepository
{
    Task<IReadOnlyList<SchoolClass>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<SchoolClass>> GetByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<IReadOnlyList<SchoolClass>> GetByStudentAsync(Guid studentId, CancellationToken ct);
    Task<SchoolClass?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(SchoolClass schoolClass, CancellationToken ct);
    Task AddEnrollmentAsync(ClassEnrollment enrollment, CancellationToken ct);
    Task<bool> RemoveEnrollmentAsync(Guid classId, Guid studentId, CancellationToken ct);
}
