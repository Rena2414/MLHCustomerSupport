using MlhSupport.Application.Classes;
using MlhSupport.Application.Common;
using MlhSupport.Application.Concerns;

namespace MlhSupport.Application.Users;

public record AuditEntryDto(Guid Id, string Actor, string Action, string Details, DateTime CreatedAt);

public record UserDetailsDto(
    UserDto User,
    IReadOnlyList<UserDto> Parents,
    IReadOnlyList<UserDto> Children,
    IReadOnlyList<ClassDto> Classes,
    IReadOnlyList<ConcernDto> Concerns,
    IReadOnlyList<AuditEntryDto> Audit);
