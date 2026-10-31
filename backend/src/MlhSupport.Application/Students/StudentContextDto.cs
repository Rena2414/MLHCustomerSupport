namespace MlhSupport.Application.Students;

public record StudentTeacherDto(Guid TeacherId, string TeacherName, Guid ClassId, string ClassName, string Subject);

public record StudentContextDto(Guid Id, string Name, int Age, int SubscriptionType, IReadOnlyList<StudentTeacherDto> Teachers);
