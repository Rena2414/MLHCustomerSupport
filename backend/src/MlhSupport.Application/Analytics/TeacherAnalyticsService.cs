using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Common;
using MlhSupport.Application.Concerns;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Analytics;

public class TeacherAnalyticsService(
    IUserRepository users,
    IClassRepository classes,
    IConcernRepository concerns,
    IAgentGateway agent)
{
    public const int TrendDays = 30;
    public const int MaxConcernsForSummary = 50;

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var teachers = await users.GetAllAsync(UserRole.Teacher, ct);
        var allConcerns = await concerns.GetAllAsync(ct);
        var allClasses = await classes.GetAllAsync(ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var stats = teachers
            .Select(t => BuildStats(
                t,
                allClasses.Where(c => c.TeacherId == t.Id).ToList(),
                allConcerns.Where(c => c.TeacherId == t.Id).ToList(),
                today))
            .OrderByDescending(s => s.OpenConcerns)
            .ThenByDescending(s => s.TotalConcerns)
            .ThenBy(s => s.TeacherName)
            .ToList();

        return new AnalyticsOverviewDto(
            allConcerns.Count,
            allConcerns.Count(c => c.Status == ConcernStatus.New),
            allConcerns.Count(IsHighOrCritical),
            CountBy(allConcerns, c => c.Category),
            stats);
    }

    public async Task<TeacherAnalyticsDetailsDto> GetTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var teacher = await RequireTeacherAsync(teacherId, ct);
        var teacherConcerns = await concerns.GetByTeacherAsync(teacher.Id, ct);
        var teacherClasses = await classes.GetByTeacherAsync(teacher.Id, ct);
        var related = (await users.GetManyAsync(teacherConcerns.Select(c => c.StudentId).Append(teacher.Id).Distinct(), ct))
            .ToDictionary(u => u.Id);

        return new TeacherAnalyticsDetailsDto(
            BuildStats(teacher, teacherClasses, teacherConcerns, DateOnly.FromDateTime(DateTime.UtcNow)),
            teacherConcerns.OrderByDescending(c => c.CreatedAt).Select(c => ConcernMapping.ToDto(c, related)).ToList());
    }

    public async Task<TeacherSummaryDto> SummarizeAsync(Guid teacherId, CancellationToken ct)
    {
        var teacher = await RequireTeacherAsync(teacherId, ct);
        var teacherConcerns = await concerns.GetByTeacherAsync(teacher.Id, ct);
        if (teacherConcerns.Count == 0)
        {
            return new TeacherSummaryDto($"No concerns have been reported about {teacher.Name}.", [], DateTime.UtcNow);
        }
        var request = new TeacherSummaryRequest(
            teacher.Name,
            teacherConcerns
                .OrderByDescending(c => c.CreatedAt)
                .Take(MaxConcernsForSummary)
                .Select(c => new TeacherSummaryConcern(c.Text, c.Category.ToString(), c.Severity.ToString(), c.CreatedAt))
                .ToList());
        return await agent.SummarizeTeacherAsync(request, ct);
    }

    private async Task<User> RequireTeacherAsync(Guid teacherId, CancellationToken ct)
    {
        var teacher = await users.GetByIdAsync(teacherId, ct);
        if (teacher is null || teacher.Role != UserRole.Teacher)
        {
            throw new NotFoundException("Teacher not found");
        }
        return teacher;
    }

    private static TeacherAnalyticsDto BuildStats(
        User teacher, IReadOnlyList<SchoolClass> teacherClasses, IReadOnlyList<TeacherConcern> teacherConcerns, DateOnly today)
    {
        var firstDay = today.AddDays(-(TrendDays - 1));
        var perDay = teacherConcerns
            .GroupBy(c => DateOnly.FromDateTime(c.CreatedAt))
            .ToDictionary(g => g.Key, g => g.Count());

        return new TeacherAnalyticsDto(
            teacher.Id,
            teacher.Name,
            teacherClasses.Count,
            teacherClasses.SelectMany(c => c.Enrollments.Select(e => e.StudentId)).Distinct().Count(),
            teacherConcerns.Count,
            teacherConcerns.Count(c => c.Status == ConcernStatus.New),
            teacherConcerns.Count(IsHighOrCritical),
            teacherConcerns.Count == 0 ? null : teacherConcerns.Max(c => c.CreatedAt),
            CountBy(teacherConcerns, c => c.Category),
            CountBy(teacherConcerns, c => c.Severity),
            Enumerable.Range(0, TrendDays)
                .Select(offset => firstDay.AddDays(offset))
                .Select(day => new DailyCountDto(day, perDay.GetValueOrDefault(day)))
                .ToList());
    }

    private static bool IsHighOrCritical(TeacherConcern concern) =>
        concern.Severity is ConcernSeverity.High or ConcernSeverity.Critical;

    private static IReadOnlyList<CountDto> CountBy<TEnum>(IEnumerable<TeacherConcern> source, Func<TeacherConcern, TEnum> key)
        where TEnum : struct, Enum
    {
        var counts = source.GroupBy(key).ToDictionary(g => g.Key, g => g.Count());
        return Enum.GetValues<TEnum>().Select(value => new CountDto(value.ToString(), counts.GetValueOrDefault(value))).ToList();
    }
}
