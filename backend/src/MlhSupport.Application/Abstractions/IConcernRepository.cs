using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Abstractions;

public interface IConcernRepository
{
    Task<IReadOnlyList<TeacherConcern>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<TeacherConcern>> GetByTeacherAsync(Guid teacherId, CancellationToken ct);
    Task<IReadOnlyList<TeacherConcern>> GetByStudentAsync(Guid studentId, CancellationToken ct);
    Task<TeacherConcern?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(TeacherConcern concern, CancellationToken ct);
    Task UpdateAsync(TeacherConcern concern, CancellationToken ct);
}
