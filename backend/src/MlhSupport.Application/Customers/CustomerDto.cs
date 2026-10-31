namespace MlhSupport.Application.Customers;

public record CustomerListItemDto(Guid Id, string FullName, string Email, string Plan, string SubscriptionStatus);

public record CustomerDetailsDto(
    Guid Id,
    string FullName,
    string Email,
    string Plan,
    string SubscriptionStatus,
    DateTime? NextPaymentDate,
    decimal? NextPaymentAmount,
    DateTime CreatedAt);
