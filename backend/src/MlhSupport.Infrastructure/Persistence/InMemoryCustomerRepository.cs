using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Infrastructure.Persistence;

public class InMemoryCustomerRepository : ICustomerRepository
{
    private static readonly IReadOnlyList<Customer> Customers =
    [
        new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), FullName = "Olena Kovalenko", Email = "olena@example.com", Plan = "Yoga Monthly", SubscriptionStatus = SubscriptionStatus.Active, NextPaymentDate = new DateTime(2026, 11, 15), NextPaymentAmount = 14.99m, CreatedAt = new DateTime(2026, 3, 2) },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), FullName = "Max Brown", Email = "max@example.com", Plan = "Wellness Annual", SubscriptionStatus = SubscriptionStatus.Trial, NextPaymentDate = new DateTime(2026, 11, 3), NextPaymentAmount = 89.00m, CreatedAt = new DateTime(2026, 10, 27) },
        new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), FullName = "Sofia Martinez", Email = "sofia@example.com", Plan = "Meditation Monthly", SubscriptionStatus = SubscriptionStatus.Cancelled, CreatedAt = new DateTime(2025, 12, 20) }
    ];

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct) => Task.FromResult(Customers);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));
}
