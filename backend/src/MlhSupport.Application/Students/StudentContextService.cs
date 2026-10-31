using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Students;

public class StudentContextService(IUserRepository users, IClassRepository classes)
{
    public async Task<StudentContextDto> GetAsync(Guid studentId, CancellationToken ct)
    {
        var student = await users.GetByIdAsync(studentId, ct);
        if (student is null || student.Role != UserRole.Student)
        {
            throw new NotFoundException("Student not found");
        }
        var studentClasses = await classes.GetByStudentAsync(student.Id, ct);
        var teachers = (await users.GetManyAsync(studentClasses.Select(c => c.TeacherId).Distinct(), ct)).ToDictionary(u => u.Id);
        return new StudentContextDto(
            student.Id,
            student.Name,
            student.Age,
            (int)student.SubscriptionType,
            studentClasses
                .Where(c => teachers.ContainsKey(c.TeacherId))
                .OrderBy(c => c.Name)
                .Select(c => new StudentTeacherDto(c.TeacherId, teachers[c.TeacherId].Name, c.Id, c.Name, c.Subject))
                .ToList());
    }
}
