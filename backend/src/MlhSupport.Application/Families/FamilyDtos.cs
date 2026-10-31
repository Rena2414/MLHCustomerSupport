using MlhSupport.Application.Classes;
using MlhSupport.Application.Common;

namespace MlhSupport.Application.Families;

public record ChildDto(UserDto Child, IReadOnlyList<ClassDto> Classes);

public record LinkChildRequest(string ChildLogin);
