using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Customers;

public class CustomerService(ICustomerRepository repository)
{
    public async Task<IReadOnlyList<CustomerListItemDto>> GetAllAsync(CancellationToken ct)
    {
        var customers = await repository.GetAllAsync(ct);
        return customers
            .Select(c => new CustomerListItemDto(c.Id, c.FullName, c.Email, c.Plan, c.SubscriptionStatus.ToString()))
            .ToList();
    }

    public async Task<CustomerDetailsDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var customer = await repository.GetByIdAsync(id, ct);
        return customer is null ? null : ToDetails(customer);
    }

    private static CustomerDetailsDto ToDetails(Customer c) => new(
        c.Id, c.FullName, c.Email, c.Plan, c.SubscriptionStatus.ToString(),
        c.NextPaymentDate, c.NextPaymentAmount, c.CreatedAt);
}
