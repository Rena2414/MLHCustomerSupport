using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Classes;
using MlhSupport.Application.Common;
using MlhSupport.Application.Concerns;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Users;

public class UserService(
    IUserRepository users,
    IFamilyRepository families,
    IClassRepository classes,
    IConcernRepository concerns,
    IAuditLog audit)
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(string? role, CancellationToken ct)
    {
        UserRole? filter = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
            {
                throw new ValidationException("Unknown role");
            }
            filter = parsed;
        }
        var list = await users.GetAllAsync(filter, ct);
        return list.OrderBy(u => u.Role).ThenBy(u => u.Name).Select(u => u.ToDto()).ToList();
    }

    public async Task<UserDetailsDto> GetDetailsAsync(Guid id, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("User not found");

        var parents = await users.GetManyAsync(await families.GetParentIdsAsync(id, ct), ct);
        var children = await users.GetManyAsync(await families.GetChildIdsAsync(id, ct), ct);

        var userClasses = user.Role switch
        {
            UserRole.Teacher => await classes.GetByTeacherAsync(id, ct),
            UserRole.Student => await classes.GetByStudentAsync(id, ct),
            _ => []
        };
        var userConcerns = user.Role switch
        {
            UserRole.Teacher => await concerns.GetByTeacherAsync(id, ct),
            UserRole.Student => await concerns.GetByStudentAsync(id, ct),
            _ => []
        };

        var relatedIds = userClasses.Select(c => c.TeacherId)
            .Concat(userClasses.SelectMany(c => c.Enrollments.Select(e => e.StudentId)))
            .Concat(userConcerns.SelectMany(c => new[] { c.StudentId, c.TeacherId }))
            .Distinct();
        var related = (await users.GetManyAsync(relatedIds, ct)).ToDictionary(u => u.Id);

        var entries = await audit.GetForUserAsync(id, ct);

        return new UserDetailsDto(
            user.ToDto(),
            parents.Select(p => p.ToDto()).ToList(),
            children.Select(c => c.ToDto()).ToList(),
            userClasses.Select(c => ClassMapping.ToDto(c, related)).ToList(),
            userConcerns.OrderByDescending(c => c.CreatedAt).Select(c => ConcernMapping.ToDto(c, related)).ToList(),
            entries.OrderByDescending(e => e.CreatedAt)
                .Select(e => new AuditEntryDto(e.Id, e.Actor, e.Action, e.Details, e.CreatedAt))
                .ToList());
    }
}
