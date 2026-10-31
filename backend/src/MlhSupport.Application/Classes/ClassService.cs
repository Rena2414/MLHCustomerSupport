using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Auth;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Classes;

public class ClassService(IClassRepository classes, IUserRepository users, IAuditLog audit)
{
    public const int MaxNameLength = 100;

    public async Task<IReadOnlyList<ClassDto>> GetVisibleAsync(Guid actorId, CancellationToken ct)
    {
        var actor = await RequireUserAsync(actorId, ct);
        var list = actor.Role switch
        {
            UserRole.Admin => await classes.GetAllAsync(ct),
            UserRole.Teacher => await classes.GetByTeacherAsync(actor.Id, ct),
            UserRole.Student => await classes.GetByStudentAsync(actor.Id, ct),
            _ => throw new ForbiddenException("Only admins, teachers and students have classes")
        };
        return await MapAsync(list, ct);
    }

    public async Task<IReadOnlyList<ClassDto>> GetForStudentAsync(Guid studentId, CancellationToken ct) =>
        await MapAsync(await classes.GetByStudentAsync(studentId, ct), ct);

    public async Task<ClassDto> CreateAsync(Guid teacherId, CreateClassRequest request, CancellationToken ct)
    {
        var teacher = await RequireUserAsync(teacherId, ct);
        if (teacher.Role != UserRole.Teacher)
        {
            throw new ForbiddenException("Only teachers can create classes");
        }
        var name = request.Name?.Trim() ?? string.Empty;
        var subject = request.Subject?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > MaxNameLength || subject.Length == 0 || subject.Length > MaxNameLength)
        {
            throw new ValidationException($"Name and subject must be between 1 and {MaxNameLength} characters");
        }

        var schoolClass = new SchoolClass
        {
            Id = Guid.NewGuid(),
            Name = name,
            Subject = subject,
            TeacherId = teacher.Id,
            CreatedAt = DateTime.UtcNow
        };
        await classes.AddAsync(schoolClass, ct);
        await audit.WriteAsync(Audit.Entry(teacher, "class.created", teacher.Id, $"Created class {name} ({subject})"), ct);
        return (await MapAsync([schoolClass], ct))[0];
    }

    public async Task<ClassDto> EnrollAsync(Guid teacherId, Guid classId, EnrollStudentRequest request, CancellationToken ct)
    {
        var (teacher, schoolClass) = await RequireOwnedClassAsync(teacherId, classId, ct);
        var student = await users.GetByLoginAsync(AuthService.NormalizeLogin(request.StudentLogin), ct);
        if (student is null || student.Role != UserRole.Student)
        {
            throw new NotFoundException("No student with this login");
        }
        if (schoolClass.Enrollments.Any(e => e.StudentId == student.Id))
        {
            throw new ConflictException("The student is already in this class");
        }

        var enrollment = new ClassEnrollment { ClassId = schoolClass.Id, StudentId = student.Id };
        await classes.AddEnrollmentAsync(enrollment, ct);
        schoolClass.Enrollments.Add(enrollment);
        await audit.WriteAsync(Audit.Entry(teacher, "class.student_added", student.Id, $"Added to class {schoolClass.Name}"), ct);
        return (await MapAsync([schoolClass], ct))[0];
    }

    public async Task RemoveAsync(Guid teacherId, Guid classId, Guid studentId, CancellationToken ct)
    {
        var (teacher, schoolClass) = await RequireOwnedClassAsync(teacherId, classId, ct);
        if (!await classes.RemoveEnrollmentAsync(classId, studentId, ct))
        {
            throw new NotFoundException("The student is not in this class");
        }
        await audit.WriteAsync(Audit.Entry(teacher, "class.student_removed", studentId, $"Removed from class {schoolClass.Name}"), ct);
    }

    private async Task<(User Teacher, SchoolClass Class)> RequireOwnedClassAsync(Guid teacherId, Guid classId, CancellationToken ct)
    {
        var teacher = await RequireUserAsync(teacherId, ct);
        var schoolClass = await classes.GetByIdAsync(classId, ct) ?? throw new NotFoundException("Class not found");
        if (schoolClass.TeacherId != teacher.Id)
        {
            throw new ForbiddenException("You can only change your own classes");
        }
        return (teacher, schoolClass);
    }

    private async Task<User> RequireUserAsync(Guid id, CancellationToken ct) =>
        await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User not found");

    private async Task<IReadOnlyList<ClassDto>> MapAsync(IReadOnlyList<SchoolClass> list, CancellationToken ct)
    {
        var ids = list.Select(c => c.TeacherId).Concat(list.SelectMany(c => c.Enrollments.Select(e => e.StudentId))).Distinct();
        var related = (await users.GetManyAsync(ids, ct)).ToDictionary(u => u.Id);
        return list.OrderBy(c => c.Name).Select(c => ClassMapping.ToDto(c, related)).ToList();
    }
}
