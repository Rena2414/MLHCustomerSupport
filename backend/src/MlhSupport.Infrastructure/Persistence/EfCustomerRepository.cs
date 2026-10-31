using Microsoft.EntityFrameworkCore;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class EfCustomerRepository(AppDbContext db) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct) =>
        await db.Customers.AsNoTracking().OrderBy(c => c.FullName).ToListAsync(ct);

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
}
