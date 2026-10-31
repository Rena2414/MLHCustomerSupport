using MlhSupport.Domain.Enums;

namespace MlhSupport.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public DateTime? NextPaymentDate { get; set; }
    public decimal? NextPaymentAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
