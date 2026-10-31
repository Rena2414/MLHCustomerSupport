using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Classes;

public record ClassStudentDto(Guid Id, string Login, string Name, int Age);

public record ClassDto(
    Guid Id,
    string Name,
    string Subject,
    Guid TeacherId,
    string TeacherName,
    DateTime CreatedAt,
    IReadOnlyList<ClassStudentDto> Students);

public record CreateClassRequest(string Name, string Subject);

public record EnrollStudentRequest(string StudentLogin);

public static class ClassMapping
{
    public static ClassDto ToDto(SchoolClass schoolClass, IReadOnlyDictionary<Guid, User> users) => new(
        schoolClass.Id,
        schoolClass.Name,
        schoolClass.Subject,
        schoolClass.TeacherId,
        users.TryGetValue(schoolClass.TeacherId, out var teacher) ? teacher.Name : string.Empty,
        schoolClass.CreatedAt,
        schoolClass.Enrollments
            .Select(e => users.GetValueOrDefault(e.StudentId))
            .OfType<User>()
            .OrderBy(s => s.Name)
            .Select(s => new ClassStudentDto(s.Id, s.Login, s.Name, s.Age))
            .ToList());
}
