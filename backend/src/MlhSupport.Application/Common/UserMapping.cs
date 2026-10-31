using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Common;

public record UserDto(Guid Id, string Login, string Name, int Age, string Role, int SubscriptionType, DateTime CreatedAt);

public static class UserMapping
{
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Login, user.Name, user.Age, user.Role.ToString(), (int)user.SubscriptionType, user.CreatedAt);
}
