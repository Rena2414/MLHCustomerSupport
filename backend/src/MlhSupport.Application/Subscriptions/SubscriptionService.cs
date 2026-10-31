using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Subscriptions;

public class SubscriptionService(IUserRepository users, IFamilyRepository families, IAuditLog audit)
{
    public async Task<UserDto> SubscribeAsync(Guid actorId, SubscribeRequest request, CancellationToken ct)
    {
        var actor = await users.GetByIdAsync(actorId, ct) ?? throw new NotFoundException("User not found");
        var target = actor.Role switch
        {
            UserRole.Student when request.ChildId is null || request.ChildId == actor.Id => actor,
            UserRole.Parent when request.ChildId is { } childId => await RequireLinkedChildAsync(actor, childId, ct),
            UserRole.Parent => throw new ValidationException("Choose which child to subscribe"),
            _ => throw new ForbiddenException("Only students and parents can subscribe")
        };

        if (target.SubscriptionType == SubscriptionType.Default)
        {
            return target.ToDto();
        }
        target.SubscriptionType = SubscriptionType.Default;
        await users.UpdateAsync(target, ct);
        await audit.WriteAsync(Audit.Entry(actor, "subscription.started", target.Id, "Subscription set to Default (1)"), ct);
        return target.ToDto();
    }

    public async Task<SubscriptionChangeDto> CancelByAgentAsync(Guid studentId, CancellationToken ct)
    {
        var student = await users.GetByIdAsync(studentId, ct) ?? throw new NotFoundException("Student not found");
        if (student.Role != UserRole.Student)
        {
            throw new ForbiddenException("Only student subscriptions can be cancelled by the support agent");
        }
        if (student.SubscriptionType == SubscriptionType.None)
        {
            return new SubscriptionChangeDto(student.Id, 0, false);
        }
        student.SubscriptionType = SubscriptionType.None;
        await users.UpdateAsync(student, ct);
        await audit.WriteAsync(
            Audit.Entry(null, "subscription.cancelled", student.Id, "Cancelled by the support agent after the student confirmed"), ct);
        return new SubscriptionChangeDto(student.Id, 0, true);
    }

    private async Task<User> RequireLinkedChildAsync(User parent, Guid childId, CancellationToken ct)
    {
        if (!await families.ExistsAsync(parent.Id, childId, ct))
        {
            throw new ForbiddenException("This child is not linked to you");
        }
        return await users.GetByIdAsync(childId, ct) ?? throw new NotFoundException("Child not found");
    }
}

public record SubscriptionChangeDto(Guid StudentId, int SubscriptionType, bool Changed);
