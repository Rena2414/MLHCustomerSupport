using Microsoft.EntityFrameworkCore;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Infrastructure.Persistence;

public static class SeedData
{
    public const string AdminPassword = "admin";
    public const string TeacherPassword = "teacher";
    public const string StudentPassword = "student";
    public const string ParentPassword = "parent";

    private record SeedUser(Guid Id, string Login, string Name, int Age, UserRole Role, SubscriptionType Subscription);

    private record SeedClass(Guid Id, string Name, string Subject, string TeacherLogin, string[] StudentLogins);

    private record SeedConcern(
        string StudentLogin,
        string TeacherLogin,
        int DaysAgo,
        ConcernCategory Category,
        ConcernSeverity Severity,
        string Text,
        string Summary,
        ConcernStatus Status = ConcernStatus.New);

    private static readonly SeedUser[] Users =
    [
        new(Id(0, 1), "admin", "Administrator", 35, UserRole.Admin, SubscriptionType.None),

        new(Id(1, 1), "ms.harper", "Olivia Harper", 41, UserRole.Teacher, SubscriptionType.None),
        new(Id(1, 2), "mr.lee", "Daniel Lee", 33, UserRole.Teacher, SubscriptionType.None),
        new(Id(1, 3), "ms.patel", "Priya Patel", 38, UserRole.Teacher, SubscriptionType.None),
        new(Id(1, 4), "mr.novak", "Tomas Novak", 52, UserRole.Teacher, SubscriptionType.None),
        new(Id(1, 5), "ms.okafor", "Grace Okafor", 29, UserRole.Teacher, SubscriptionType.None),

        new(Id(2, 1), "emma", "Emma Wilson", 10, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 2), "liam", "Liam Wilson", 13, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 3), "noah", "Noah Garcia", 9, UserRole.Student, SubscriptionType.None),
        new(Id(2, 4), "mia", "Mia Garcia", 15, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 5), "ava", "Ava Brown", 11, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 6), "lucas", "Lucas Brown", 12, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 7), "sofia", "Sofia Rossi", 10, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 8), "ethan", "Ethan Rossi", 14, UserRole.Student, SubscriptionType.None),
        new(Id(2, 9), "chloe", "Chloe Chen", 9, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 10), "mason", "Mason Chen", 13, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 11), "zoe", "Zoe Miller", 12, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 12), "oliver", "Oliver Miller", 15, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 13), "lily", "Lily Kim", 11, UserRole.Student, SubscriptionType.None),
        new(Id(2, 14), "jack", "Jack Kim", 10, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 15), "ella", "Ella Diaz", 14, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 16), "leo", "Leo Diaz", 9, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 17), "grace", "Grace Evans", 13, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 18), "henry", "Henry Evans", 12, UserRole.Student, SubscriptionType.None),
        new(Id(2, 19), "amelia", "Amelia White", 15, UserRole.Student, SubscriptionType.Default),
        new(Id(2, 20), "ben", "Ben White", 11, UserRole.Student, SubscriptionType.Default),

        new(Id(3, 1), "anna", "Anna Wilson", 39, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 2), "david", "David Garcia", 44, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 3), "kate.brown", "Kate Brown", 37, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 4), "maria.rossi", "Maria Rossi", 42, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 5), "james.chen", "James Chen", 40, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 6), "sarah.miller", "Sarah Miller", 45, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 7), "peter.kim", "Peter Kim", 39, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 8), "laura.diaz", "Laura Diaz", 36, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 9), "tom.evans", "Tom Evans", 47, UserRole.Parent, SubscriptionType.None),
        new(Id(3, 10), "nina.white", "Nina White", 43, UserRole.Parent, SubscriptionType.None)
    ];

    private static readonly (string Parent, string Child)[] Families =
    [
        ("anna", "emma"), ("anna", "liam"),
        ("david", "noah"), ("david", "mia"),
        ("kate.brown", "ava"), ("kate.brown", "lucas"),
        ("maria.rossi", "sofia"), ("maria.rossi", "ethan"),
        ("james.chen", "chloe"), ("james.chen", "mason"),
        ("sarah.miller", "zoe"), ("sarah.miller", "oliver"),
        ("peter.kim", "lily"), ("peter.kim", "jack"),
        ("laura.diaz", "ella"), ("laura.diaz", "leo"),
        ("tom.evans", "grace"), ("tom.evans", "henry"),
        ("nina.white", "amelia"), ("nina.white", "ben")
    ];

    private static readonly SeedClass[] Classes =
    [
        new(Id(4, 1), "Math 5A", "Mathematics", "ms.harper", ["emma", "liam", "ava", "sofia", "chloe", "jack", "lily"]),
        new(Id(4, 2), "English 5A", "English", "ms.harper", ["emma", "mia", "zoe", "ella", "ben"]),
        new(Id(4, 3), "Science 7B", "Science", "mr.lee", ["liam", "noah", "mia", "lucas", "ethan", "mason", "oliver", "henry"]),
        new(Id(4, 4), "History 6B", "History", "ms.patel", ["ava", "ethan", "grace", "amelia", "leo", "noah"]),
        new(Id(4, 5), "Art 4C", "Art", "ms.patel", ["chloe", "leo", "lily", "jack", "ben"]),
        new(Id(4, 6), "Geography 7A", "Geography", "mr.novak", ["oliver", "henry", "amelia", "zoe", "mason", "lucas"]),
        new(Id(4, 7), "Music 5B", "Music", "ms.okafor", ["sofia", "ella", "grace", "emma", "noah", "mia"]),
        new(Id(4, 8), "Computer Science 8A", "Computer Science", "ms.okafor", ["ethan", "oliver", "amelia", "liam", "henry"])
    ];

    private static readonly SeedConcern[] Concerns =
    [
        new("liam", "mr.lee", 27, ConcernCategory.Communication, ConcernSeverity.Medium, "Mr. Lee did not answer my questions about the lab report for two weeks.", "Questions about the lab report stayed unanswered for two weeks.", ConcernStatus.Reviewed),
        new("noah", "mr.lee", 21, ConcernCategory.LatenessOrAbsence, ConcernSeverity.Medium, "The lesson started 20 minutes late again and we did not finish the experiment.", "Lessons start late and experiments are not finished.", ConcernStatus.Reviewed),
        new("mia", "mr.lee", 15, ConcernCategory.Fairness, ConcernSeverity.High, "I got a lower grade than my friend for the same answers and he would not explain why.", "Different grades for the same answers without explanation."),
        new("liam", "mr.lee", 11, ConcernCategory.LatenessOrAbsence, ConcernSeverity.Low, "He was late to class three times this week.", "Teacher late to class three times in one week."),
        new("noah", "mr.lee", 8, ConcernCategory.Behavior, ConcernSeverity.High, "Mr. Lee shouted at me in front of everyone when I asked to repeat the task.", "Student was shouted at in front of the class for asking a question."),
        new("mia", "mr.lee", 5, ConcernCategory.TeachingQuality, ConcernSeverity.Medium, "The explanations go too fast and nobody understands the homework.", "Explanations are too fast and the homework is unclear."),
        new("liam", "mr.lee", 3, ConcernCategory.Communication, ConcernSeverity.Low, "Our messages in the class chat are ignored.", "Class chat messages are ignored."),
        new("noah", "mr.lee", 1, ConcernCategory.LatenessOrAbsence, ConcernSeverity.Medium, "We did not have the lesson today and nobody told us.", "Lesson was missed without notice."),
        new("lucas", "mr.lee", 18, ConcernCategory.TeachingQuality, ConcernSeverity.Medium, "We only read from the slides and never do the experiments.", "Lessons are slide reading without practical work."),
        new("mason", "mr.lee", 9, ConcernCategory.Behavior, ConcernSeverity.Medium, "He makes jokes about students who get answers wrong.", "Teacher mocks students for wrong answers."),
        new("oliver", "mr.lee", 6, ConcernCategory.Fairness, ConcernSeverity.Medium, "Only his favourite students get to present their projects.", "Presentation opportunities given only to some students."),
        new("ethan", "mr.lee", 2, ConcernCategory.Communication, ConcernSeverity.Low, "The homework deadline changed and he did not tell us.", "Homework deadline changed without notice."),

        new("emma", "ms.harper", 19, ConcernCategory.TeachingQuality, ConcernSeverity.Low, "Ms. Harper gives a lot of homework on weekends.", "Large amount of weekend homework.", ConcernStatus.Reviewed),
        new("mia", "ms.harper", 9, ConcernCategory.Fairness, ConcernSeverity.Medium, "She marked my essay late even though I sent it on time.", "Essay marked late although it was submitted on time."),
        new("liam", "ms.harper", 4, ConcernCategory.TeachingQuality, ConcernSeverity.Low, "The math test had topics we never studied.", "Test included topics not covered in class."),
        new("sofia", "ms.harper", 12, ConcernCategory.Communication, ConcernSeverity.Low, "I asked for help with fractions but did not get an answer.", "Request for help with fractions was not answered."),

        new("ava", "ms.patel", 22, ConcernCategory.TeachingQuality, ConcernSeverity.Low, "The history videos are very long and boring.", "Long lesson videos are hard to follow.", ConcernStatus.Reviewed),
        new("leo", "ms.patel", 7, ConcernCategory.LatenessOrAbsence, ConcernSeverity.Low, "Art class was cancelled two times this month.", "Art class cancelled twice in one month."),
        new("grace", "ms.patel", 3, ConcernCategory.Fairness, ConcernSeverity.Medium, "My group got a lower mark because of one person who did nothing.", "Group mark penalised the whole group for one member."),

        new("oliver", "mr.novak", 25, ConcernCategory.Behavior, ConcernSeverity.High, "Mr. Novak called me lazy in front of the whole class.", "Student called lazy in front of the class."),
        new("amelia", "mr.novak", 20, ConcernCategory.Communication, ConcernSeverity.Medium, "He never replies to messages from students or parents.", "Messages from students and parents get no reply.", ConcernStatus.Reviewed),
        new("henry", "mr.novak", 14, ConcernCategory.Safety, ConcernSeverity.Critical, "Mr. Novak grabbed my arm hard when I was talking in class and it hurt.", "Student reports being grabbed hard by the arm and hurt."),
        new("zoe", "mr.novak", 10, ConcernCategory.TeachingQuality, ConcernSeverity.Medium, "We copy maps from the book for the whole lesson and he does not explain anything.", "Lessons are copying exercises without explanation."),
        new("mason", "mr.novak", 6, ConcernCategory.Fairness, ConcernSeverity.Medium, "He gave me zero because my camera was off for five minutes.", "Zero grade given for a short camera-off period."),
        new("lucas", "mr.novak", 2, ConcernCategory.Behavior, ConcernSeverity.High, "He said our class is the worst he has ever taught.", "Teacher insulted the whole class."),

        new("ella", "ms.okafor", 16, ConcernCategory.LatenessOrAbsence, ConcernSeverity.Low, "Music class started ten minutes late.", "Lesson started ten minutes late.", ConcernStatus.Reviewed),
        new("ethan", "ms.okafor", 5, ConcernCategory.Other, ConcernSeverity.Low, "The coding tasks are too easy for me and I get bored.", "Coding tasks are not challenging enough.")
    ];

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var passwordHashes = new Dictionary<UserRole, string>
        {
            [UserRole.Admin] = hasher.Hash(AdminPassword),
            [UserRole.Teacher] = hasher.Hash(TeacherPassword),
            [UserRole.Student] = hasher.Hash(StudentPassword),
            [UserRole.Parent] = hasher.Hash(ParentPassword)
        };

        var existingLogins = await db.Users.Select(u => u.Login).ToListAsync(ct);
        db.Users.AddRange(Users
            .Where(u => !existingLogins.Contains(u.Login))
            .Select(u => new User
            {
                Id = u.Id,
                Login = u.Login,
                PasswordHash = passwordHashes[u.Role],
                Name = u.Name,
                Age = u.Age,
                Role = u.Role,
                SubscriptionType = u.Subscription,
                CreatedAt = now.AddDays(-60)
            }));
        await db.SaveChangesAsync(ct);

        var ids = await db.Users.ToDictionaryAsync(u => u.Login, u => u.Id, ct);

        var existingLinks = await db.ParentChildren.Select(p => new { p.ParentId, p.ChildId }).ToListAsync(ct);
        db.ParentChildren.AddRange(Families
            .Select(f => new ParentChild { ParentId = ids[f.Parent], ChildId = ids[f.Child] })
            .Where(l => !existingLinks.Any(e => e.ParentId == l.ParentId && e.ChildId == l.ChildId)));

        var existingClassIds = await db.Classes.Select(c => c.Id).ToListAsync(ct);
        db.Classes.AddRange(Classes
            .Where(c => !existingClassIds.Contains(c.Id))
            .Select(c => new SchoolClass
            {
                Id = c.Id,
                Name = c.Name,
                Subject = c.Subject,
                TeacherId = ids[c.TeacherLogin],
                CreatedAt = now.AddDays(-45)
            }));

        var existingEnrollments = await db.ClassEnrollments.Select(e => new { e.ClassId, e.StudentId }).ToListAsync(ct);
        db.ClassEnrollments.AddRange(Classes
            .SelectMany(c => c.StudentLogins.Select(s => new ClassEnrollment { ClassId = c.Id, StudentId = ids[s] }))
            .Where(e => !existingEnrollments.Any(x => x.ClassId == e.ClassId && x.StudentId == e.StudentId)));

        var existingConcerns = await db.TeacherConcerns.Select(c => new { c.StudentId, c.TeacherId, c.Text }).ToListAsync(ct);
        db.TeacherConcerns.AddRange(Concerns
            .Where(c => !existingConcerns.Any(e =>
                e.StudentId == ids[c.StudentLogin] && e.TeacherId == ids[c.TeacherLogin] && e.Text == c.Text))
            .Select(c => new TeacherConcern
            {
                Id = Guid.NewGuid(),
                StudentId = ids[c.StudentLogin],
                TeacherId = ids[c.TeacherLogin],
                Text = c.Text,
                Category = c.Category,
                Severity = c.Severity,
                Summary = c.Summary,
                Status = c.Status,
                CreatedAt = now.AddDays(-c.DaysAgo).AddHours(-c.DaysAgo % 7)
            }));

        await db.SaveChangesAsync(ct);
    }

    private static Guid Id(int group, int number) => Guid.Parse($"{group}0000000-0000-0000-0000-{number:D12}");
}
