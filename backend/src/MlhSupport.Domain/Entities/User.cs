using MlhSupport.Domain.Enums;

namespace MlhSupport.Domain.Entities;

public class User
{
    public const int MinAge = 5;
    public const int MaxAge = 120;

    public Guid Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public UserRole Role { get; set; }
    public SubscriptionType SubscriptionType { get; set; }
    public DateTime CreatedAt { get; set; }
}
