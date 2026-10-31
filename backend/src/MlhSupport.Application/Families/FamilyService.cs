using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Auth;
using MlhSupport.Application.Classes;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Families;

public class FamilyService(IFamilyRepository families, IUserRepository users, ClassService classes, IAuditLog audit)
{
    public async Task<IReadOnlyList<ChildDto>> GetChildrenAsync(Guid parentId, CancellationToken ct)
    {
        await RequireParentAsync(parentId, ct);
        var children = await users.GetManyAsync(await families.GetChildIdsAsync(parentId, ct), ct);
        var result = new List<ChildDto>();
        foreach (var child in children.OrderBy(c => c.Name))
        {
            result.Add(new ChildDto(child.ToDto(), await classes.GetForStudentAsync(child.Id, ct)));
        }
        return result;
    }

    public async Task<ChildDto> LinkChildAsync(Guid parentId, LinkChildRequest request, CancellationToken ct)
    {
        var parent = await RequireParentAsync(parentId, ct);
        var child = await users.GetByLoginAsync(AuthService.NormalizeLogin(request.ChildLogin), ct);
        if (child is null || child.Role != UserRole.Student)
        {
            throw new NotFoundException("No student with this login");
        }
        if (await families.ExistsAsync(parent.Id, child.Id, ct))
        {
            throw new ConflictException("This child is already linked");
        }

        await families.AddAsync(new ParentChild { ParentId = parent.Id, ChildId = child.Id }, ct);
        await audit.WriteAsync(Audit.Entry(parent, "family.child_linked", child.Id, $"Linked to parent {parent.Name}"), ct);
        return new ChildDto(child.ToDto(), await classes.GetForStudentAsync(child.Id, ct));
    }

    public async Task UnlinkChildAsync(Guid parentId, Guid childId, CancellationToken ct)
    {
        var parent = await RequireParentAsync(parentId, ct);
        if (!await families.RemoveAsync(parent.Id, childId, ct))
        {
            throw new NotFoundException("This child is not linked");
        }
        await audit.WriteAsync(Audit.Entry(parent, "family.child_unlinked", childId, $"Unlinked from parent {parent.Name}"), ct);
    }

    private async Task<User> RequireParentAsync(Guid parentId, CancellationToken ct)
    {
        var parent = await users.GetByIdAsync(parentId, ct) ?? throw new NotFoundException("User not found");
        if (parent.Role != UserRole.Parent)
        {
            throw new ForbiddenException("Only parents can manage children");
        }
        return parent;
    }
}
