using System.Runtime.CompilerServices;
using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Analytics;
using MlhSupport.Application.Conversations;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Tests;

public class InMemoryStore : IUserRepository, IFamilyRepository, IClassRepository, IConcernRepository, IAuditLog, IConversationRepository
{
    public List<User> Users { get; } = [];
    public List<ParentChild> Links { get; } = [];
    public List<SchoolClass> Classes { get; } = [];
    public List<TeacherConcern> Concerns { get; } = [];
    public List<AuditEntry> AuditEntries { get; } = [];
    public Dictionary<Guid, Conversation> Conversations { get; } = new();

    public User AddUser(string login, UserRole role, SubscriptionType subscription = SubscriptionType.None, int age = 12)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHash = "hash:" + login,
            Name = login.ToUpperInvariant(),
            Age = age,
            Role = role,
            SubscriptionType = subscription,
            CreatedAt = DateTime.UtcNow
        };
        Users.Add(user);
        return user;
    }

    public SchoolClass AddClass(User teacher, params User[] students)
    {
        var id = Guid.NewGuid();
        var schoolClass = new SchoolClass
        {
            Id = id,
            Name = "Class " + Classes.Count,
            Subject = "Math",
            TeacherId = teacher.Id,
            CreatedAt = DateTime.UtcNow,
            Enrollments = students.Select(s => new ClassEnrollment { ClassId = id, StudentId = s.Id }).ToList()
        };
        Classes.Add(schoolClass);
        return schoolClass;
    }

    Task<User?> IUserRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    public Task<User?> GetByLoginAsync(string login, CancellationToken ct) => Task.FromResult(Users.FirstOrDefault(u => u.Login == login));
    public Task<IReadOnlyList<User>> GetAllAsync(UserRole? role, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<User>>(Users.Where(u => role == null || u.Role == role).ToList());
    public Task<IReadOnlyList<User>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var set = ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<User>>(Users.Where(u => set.Contains(u.Id)).ToList());
    }
    public Task AddAsync(User user, CancellationToken ct)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }
    public Task UpdateAsync(User user, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<Guid>> GetChildIdsAsync(Guid parentId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Guid>>(Links.Where(l => l.ParentId == parentId).Select(l => l.ChildId).ToList());
    public Task<IReadOnlyList<Guid>> GetParentIdsAsync(Guid childId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Guid>>(Links.Where(l => l.ChildId == childId).Select(l => l.ParentId).ToList());
    public Task<bool> ExistsAsync(Guid parentId, Guid childId, CancellationToken ct) =>
        Task.FromResult(Links.Any(l => l.ParentId == parentId && l.ChildId == childId));
    public Task AddAsync(ParentChild link, CancellationToken ct)
    {
        Links.Add(link);
        return Task.CompletedTask;
    }
    public Task<bool> RemoveAsync(Guid parentId, Guid childId, CancellationToken ct) =>
        Task.FromResult(Links.RemoveAll(l => l.ParentId == parentId && l.ChildId == childId) > 0);

    Task<IReadOnlyList<SchoolClass>> IClassRepository.GetAllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<SchoolClass>>(Classes.ToList());
    Task<IReadOnlyList<SchoolClass>> IClassRepository.GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SchoolClass>>(Classes.Where(c => c.TeacherId == teacherId).ToList());
    Task<IReadOnlyList<SchoolClass>> IClassRepository.GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SchoolClass>>(Classes.Where(c => c.Enrollments.Any(e => e.StudentId == studentId)).ToList());
    Task<SchoolClass?> IClassRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Classes.FirstOrDefault(c => c.Id == id));
    public Task AddAsync(SchoolClass schoolClass, CancellationToken ct)
    {
        Classes.Add(schoolClass);
        return Task.CompletedTask;
    }
    public Task AddEnrollmentAsync(ClassEnrollment enrollment, CancellationToken ct) => Task.CompletedTask;
    public Task<bool> RemoveEnrollmentAsync(Guid classId, Guid studentId, CancellationToken ct) =>
        Task.FromResult(Classes.Single(c => c.Id == classId).Enrollments.RemoveAll(e => e.StudentId == studentId) > 0);

    Task<IReadOnlyList<TeacherConcern>> IConcernRepository.GetAllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<TeacherConcern>>(Concerns.ToList());
    Task<IReadOnlyList<TeacherConcern>> IConcernRepository.GetByTeacherAsync(Guid teacherId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TeacherConcern>>(Concerns.Where(c => c.TeacherId == teacherId).ToList());
    Task<IReadOnlyList<TeacherConcern>> IConcernRepository.GetByStudentAsync(Guid studentId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TeacherConcern>>(Concerns.Where(c => c.StudentId == studentId).ToList());
    Task<TeacherConcern?> IConcernRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Concerns.FirstOrDefault(c => c.Id == id));
    public Task AddAsync(TeacherConcern concern, CancellationToken ct)
    {
        Concerns.Add(concern);
        return Task.CompletedTask;
    }
    public Task UpdateAsync(TeacherConcern concern, CancellationToken ct) => Task.CompletedTask;

    public Task WriteAsync(AuditEntry entry, CancellationToken ct)
    {
        AuditEntries.Add(entry);
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<AuditEntry>> GetForUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AuditEntry>>(AuditEntries.Where(a => a.TargetUserId == userId || a.ActorUserId == userId).ToList());

    public Task<IReadOnlyList<Conversation>> GetByUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Conversation>>(Conversations.Values.Where(c => c.UserId == userId).ToList());
    Task<Conversation?> IConversationRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Conversations.GetValueOrDefault(id));
    public Task AddAsync(Conversation conversation, CancellationToken ct)
    {
        Conversations[conversation.Id] = conversation;
        return Task.CompletedTask;
    }
    public Task AddMessageAsync(Conversation conversation, ChatMessage message, CancellationToken ct)
    {
        conversation.Messages.Add(message);
        return Task.CompletedTask;
    }
}

public class FakeAgent(Func<string, string>? reply = null) : IAgentGateway
{
    public List<(Guid ConversationId, AgentUserContext User, string Text)> Calls { get; } = [];
    public List<TeacherSummaryRequest> SummaryRequests { get; } = [];
    public bool Fail { get; set; }

    public Task<string> GetReplyAsync(Guid conversationId, AgentUserContext user, string text, CancellationToken ct)
    {
        Calls.Add((conversationId, user, text));
        if (Fail)
        {
            throw new AgentUnavailableException("down");
        }
        return Task.FromResult(Reply(text));
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        Guid conversationId, AgentUserContext user, string text, [EnumeratorCancellation] CancellationToken ct)
    {
        Calls.Add((conversationId, user, text));
        await Task.Yield();
        if (Fail)
        {
            throw new AgentUnavailableException("down");
        }
        foreach (var piece in Reply(text).Split(' ').Select((word, index) => index == 0 ? word : " " + word))
        {
            yield return piece;
        }
    }

    public Task<TeacherSummaryDto> SummarizeTeacherAsync(TeacherSummaryRequest request, CancellationToken ct)
    {
        SummaryRequests.Add(request);
        return Task.FromResult(new TeacherSummaryDto("summary for " + request.TeacherName, ["talk to the teacher"], DateTime.UtcNow));
    }

    private string Reply(string text) => reply?.Invoke(text) ?? "echo: " + text;
}

public class FakeHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string hash, string password) => hash == "hash:" + password;
}

public class FakeTokens : ITokenService
{
    public string CreateToken(User user) => "token:" + user.Login;
}
