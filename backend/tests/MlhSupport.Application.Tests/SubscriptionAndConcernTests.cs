using MlhSupport.Application.Analytics;
using MlhSupport.Application.Common;
using MlhSupport.Application.Concerns;
using MlhSupport.Application.Students;
using MlhSupport.Application.Subscriptions;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Tests;

public class SubscriptionAndConcernTests
{
    [Fact]
    public async Task AgentCancel_SetsSubscriptionToZeroOnceAndAudits()
    {
        var store = new InMemoryStore();
        var student = store.AddUser("emma", UserRole.Student, SubscriptionType.Default);
        var service = new SubscriptionService(store, store, store);

        var first = await service.CancelByAgentAsync(student.Id, CancellationToken.None);
        var second = await service.CancelByAgentAsync(student.Id, CancellationToken.None);

        Assert.True(first.Changed);
        Assert.False(second.Changed);
        Assert.Equal(SubscriptionType.None, student.SubscriptionType);
        var entry = Assert.Single(store.AuditEntries);
        Assert.Equal("subscription.cancelled", entry.Action);
        Assert.Equal(Audit.AgentActor, entry.Actor);
    }

    [Fact]
    public async Task AgentCancel_RejectsNonStudents()
    {
        var store = new InMemoryStore();
        var teacher = store.AddUser("harper", UserRole.Teacher, SubscriptionType.Default);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => new SubscriptionService(store, store, store).CancelByAgentAsync(teacher.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Parent_SubscribesOnlyLinkedChild()
    {
        var store = new InMemoryStore();
        var parent = store.AddUser("anna", UserRole.Parent);
        var child = store.AddUser("emma", UserRole.Student);
        var stranger = store.AddUser("noah", UserRole.Student);
        store.Links.Add(new ParentChild { ParentId = parent.Id, ChildId = child.Id });
        var service = new SubscriptionService(store, store, store);

        var result = await service.SubscribeAsync(parent.Id, new SubscribeRequest(child.Id), CancellationToken.None);

        Assert.Equal(1, result.SubscriptionType);
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.SubscribeAsync(parent.Id, new SubscribeRequest(stranger.Id), CancellationToken.None));
        Assert.Equal(SubscriptionType.None, stranger.SubscriptionType);
    }

    [Fact]
    public async Task ReportConcern_StoresClassifiedConcernForOwnTeacher()
    {
        var store = new InMemoryStore();
        var teacher = store.AddUser("lee", UserRole.Teacher);
        var student = store.AddUser("noah", UserRole.Student);
        store.AddClass(teacher, student);
        var service = new ConcernService(store, store, store, store);

        var concern = await service.ReportByAgentAsync(
            new ReportConcernRequest(student.Id, teacher.Id, null, "He shouted at me", "behavior", "HIGH", "Shouting"),
            CancellationToken.None);

        Assert.Equal("Behavior", concern.Category);
        Assert.Equal("High", concern.Severity);
        Assert.Equal("New", concern.Status);
        Assert.Single(store.Concerns);
    }

    [Fact]
    public async Task ReportConcern_RejectsTeacherWhoDoesNotTeachTheStudent()
    {
        var store = new InMemoryStore();
        var teacher = store.AddUser("lee", UserRole.Teacher);
        var student = store.AddUser("noah", UserRole.Student);
        var service = new ConcernService(store, store, store, store);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ReportByAgentAsync(
            new ReportConcernRequest(student.Id, teacher.Id, null, "text", "Other", "Low", "s"), CancellationToken.None));
        Assert.Empty(store.Concerns);
    }

    [Fact]
    public async Task ReportConcern_UnknownLabelsFallBackToOtherAndMedium()
    {
        var store = new InMemoryStore();
        var teacher = store.AddUser("lee", UserRole.Teacher);
        var student = store.AddUser("noah", UserRole.Student);
        store.AddClass(teacher, student);

        var concern = await new ConcernService(store, store, store, store).ReportByAgentAsync(
            new ReportConcernRequest(student.Id, teacher.Id, null, "text", "weird", "huge", "s"), CancellationToken.None);

        Assert.Equal("Other", concern.Category);
        Assert.Equal("Medium", concern.Severity);
    }

    [Fact]
    public async Task StudentContext_ListsTeachersOfTheirClasses()
    {
        var store = new InMemoryStore();
        var teacher = store.AddUser("lee", UserRole.Teacher);
        var student = store.AddUser("noah", UserRole.Student, SubscriptionType.Default, age: 9);
        store.AddClass(teacher, student);
        store.AddClass(store.AddUser("harper", UserRole.Teacher));

        var context = await new StudentContextService(store, store).GetAsync(student.Id, CancellationToken.None);

        Assert.Equal(1, context.SubscriptionType);
        Assert.Equal(9, context.Age);
        Assert.Equal(teacher.Id, Assert.Single(context.Teachers).TeacherId);
    }

    [Fact]
    public async Task Analytics_AggregatesPerTeacherAndRanksOpenConcernsFirst()
    {
        var store = new InMemoryStore();
        var harper = store.AddUser("harper", UserRole.Teacher);
        var lee = store.AddUser("lee", UserRole.Teacher);
        var student = store.AddUser("noah", UserRole.Student);
        store.AddClass(lee, student);
        var now = DateTime.UtcNow;
        store.Concerns.AddRange(
        [
            Concern(student, lee, ConcernCategory.Behavior, ConcernSeverity.Critical, now, ConcernStatus.New),
            Concern(student, lee, ConcernCategory.Behavior, ConcernSeverity.Low, now.AddDays(-2), ConcernStatus.New),
            Concern(student, lee, ConcernCategory.Fairness, ConcernSeverity.High, now.AddDays(-40), ConcernStatus.Reviewed),
            Concern(student, harper, ConcernCategory.Other, ConcernSeverity.Low, now, ConcernStatus.Reviewed)
        ]);
        var agent = new FakeAgent();
        var service = new TeacherAnalyticsService(store, store, store, agent);

        var overview = await service.GetOverviewAsync(CancellationToken.None);

        Assert.Equal(4, overview.TotalConcerns);
        Assert.Equal(2, overview.OpenConcerns);
        Assert.Equal(2, overview.HighOrCriticalConcerns);
        var top = overview.Teachers[0];
        Assert.Equal(lee.Id, top.TeacherId);
        Assert.Equal(3, top.TotalConcerns);
        Assert.Equal(1, top.StudentCount);
        Assert.Equal(2, top.ByCategory.Single(c => c.Key == "Behavior").Count);
        Assert.Equal(TeacherAnalyticsService.TrendDays, top.Last30Days.Count);
        Assert.Equal(2, top.Last30Days.Sum(d => d.Count));

        var summary = await service.SummarizeAsync(lee.Id, CancellationToken.None);
        Assert.Equal("summary for LEE", summary.Summary);
        Assert.Equal(3, Assert.Single(agent.SummaryRequests).Concerns.Count);
    }

    [Fact]
    public async Task Analytics_SummaryWithoutConcerns_DoesNotCallAgent()
    {
        var store = new InMemoryStore();
        var teacher = store.AddUser("harper", UserRole.Teacher);
        var agent = new FakeAgent();

        var summary = await new TeacherAnalyticsService(store, store, store, agent).SummarizeAsync(teacher.Id, CancellationToken.None);

        Assert.Contains("No concerns", summary.Summary);
        Assert.Empty(agent.SummaryRequests);
    }

    private static TeacherConcern Concern(
        User student, User teacher, ConcernCategory category, ConcernSeverity severity, DateTime createdAt, ConcernStatus status) => new()
    {
        Id = Guid.NewGuid(),
        StudentId = student.Id,
        TeacherId = teacher.Id,
        Text = "text",
        Category = category,
        Severity = severity,
        Summary = "summary",
        Status = status,
        CreatedAt = createdAt
    };
}
