using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Concerns;

public class ConcernService(IConcernRepository concerns, IClassRepository classes, IUserRepository users, IAuditLog audit)
{
    public const int MaxTextLength = 4000;
    public const int MaxSummaryLength = 500;

    public async Task<ConcernDto> ReportByAgentAsync(ReportConcernRequest request, CancellationToken ct)
    {
        var text = request.Text?.Trim() ?? string.Empty;
        if (text.Length == 0 || text.Length > MaxTextLength)
        {
            throw new ValidationException($"The concern must be between 1 and {MaxTextLength} characters");
        }
        var student = await users.GetByIdAsync(request.StudentId, ct);
        if (student is null || student.Role != UserRole.Student)
        {
            throw new NotFoundException("Student not found");
        }
        var teacher = await users.GetByIdAsync(request.TeacherId, ct);
        if (teacher is null || teacher.Role != UserRole.Teacher)
        {
            throw new NotFoundException("Teacher not found");
        }
        var studentClasses = await classes.GetByStudentAsync(student.Id, ct);
        if (studentClasses.All(c => c.TeacherId != teacher.Id))
        {
            throw new ForbiddenException("This teacher does not teach the student");
        }

        var summary = request.Summary?.Trim() ?? string.Empty;
        var concern = new TeacherConcern
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            TeacherId = teacher.Id,
            ConversationId = request.ConversationId,
            Text = text,
            Category = Enum.TryParse<ConcernCategory>(request.Category, ignoreCase: true, out var category) ? category : ConcernCategory.Other,
            Severity = Enum.TryParse<ConcernSeverity>(request.Severity, ignoreCase: true, out var severity) ? severity : ConcernSeverity.Medium,
            Summary = summary.Length > MaxSummaryLength ? summary[..MaxSummaryLength] : summary,
            Status = ConcernStatus.New,
            CreatedAt = DateTime.UtcNow
        };
        await concerns.AddAsync(concern, ct);
        await audit.WriteAsync(
            Audit.Entry(null, "concern.reported", student.Id, $"Concern about {teacher.Name}: {concern.Category}, {concern.Severity}"), ct);
        return ConcernMapping.ToDto(concern, new Dictionary<Guid, User> { [student.Id] = student, [teacher.Id] = teacher });
    }

    public async Task<ConcernDto> UpdateStatusAsync(Guid adminId, Guid concernId, UpdateConcernRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ConcernStatus>(request.Status, ignoreCase: true, out var status))
        {
            throw new ValidationException("Status must be New or Reviewed");
        }
        var admin = await users.GetByIdAsync(adminId, ct) ?? throw new NotFoundException("User not found");
        var concern = await concerns.GetByIdAsync(concernId, ct) ?? throw new NotFoundException("Concern not found");
        concern.Status = status;
        await concerns.UpdateAsync(concern, ct);
        await audit.WriteAsync(Audit.Entry(admin, "concern.status_changed", concern.TeacherId, $"Concern {concern.Id} marked {status}"), ct);
        var related = (await users.GetManyAsync([concern.StudentId, concern.TeacherId], ct)).ToDictionary(u => u.Id);
        return ConcernMapping.ToDto(concern, related);
    }
}
