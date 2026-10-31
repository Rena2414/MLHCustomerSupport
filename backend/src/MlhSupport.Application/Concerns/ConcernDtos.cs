using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Concerns;

public record ConcernDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid TeacherId,
    string TeacherName,
    string Text,
    string Category,
    string Severity,
    string Summary,
    string Status,
    DateTime CreatedAt);

public record ReportConcernRequest(
    Guid StudentId,
    Guid TeacherId,
    Guid? ConversationId,
    string Text,
    string Category,
    string Severity,
    string Summary);

public record UpdateConcernRequest(string Status);

public static class ConcernMapping
{
    public static ConcernDto ToDto(TeacherConcern concern, IReadOnlyDictionary<Guid, User> users) => new(
        concern.Id,
        concern.StudentId,
        users.TryGetValue(concern.StudentId, out var student) ? student.Name : string.Empty,
        concern.TeacherId,
        users.TryGetValue(concern.TeacherId, out var teacher) ? teacher.Name : string.Empty,
        concern.Text,
        concern.Category.ToString(),
        concern.Severity.ToString(),
        concern.Summary,
        concern.Status.ToString(),
        concern.CreatedAt);
}
