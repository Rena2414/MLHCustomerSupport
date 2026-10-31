using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class InMemoryCustomerRepository : ICustomerRepository
{
    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct) => Task.FromResult(SeedData.Customers);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(SeedData.Customers.FirstOrDefault(c => c.Id == id));
}
