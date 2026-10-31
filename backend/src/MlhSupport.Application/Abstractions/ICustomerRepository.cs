using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Abstractions;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct);
}
