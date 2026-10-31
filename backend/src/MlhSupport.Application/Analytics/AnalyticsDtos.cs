using MlhSupport.Application.Concerns;

namespace MlhSupport.Application.Analytics;

public record CountDto(string Key, int Count);

public record DailyCountDto(DateOnly Date, int Count);

public record TeacherAnalyticsDto(
    Guid TeacherId,
    string TeacherName,
    int ClassCount,
    int StudentCount,
    int TotalConcerns,
    int OpenConcerns,
    int HighOrCriticalConcerns,
    DateTime? LastConcernAt,
    IReadOnlyList<CountDto> ByCategory,
    IReadOnlyList<CountDto> BySeverity,
    IReadOnlyList<DailyCountDto> Last30Days);

public record AnalyticsOverviewDto(
    int TotalConcerns,
    int OpenConcerns,
    int HighOrCriticalConcerns,
    IReadOnlyList<CountDto> ByCategory,
    IReadOnlyList<TeacherAnalyticsDto> Teachers);

public record TeacherAnalyticsDetailsDto(TeacherAnalyticsDto Stats, IReadOnlyList<ConcernDto> Concerns);

public record TeacherSummaryConcern(string Text, string Category, string Severity, DateTime CreatedAt);

public record TeacherSummaryRequest(string TeacherName, IReadOnlyList<TeacherSummaryConcern> Concerns);

public record TeacherSummaryDto(string Summary, IReadOnlyList<string> RecommendedActions, DateTime GeneratedAt);
