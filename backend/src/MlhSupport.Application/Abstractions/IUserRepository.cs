using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByLoginAsync(string login, CancellationToken ct);
    Task<IReadOnlyList<User>> GetAllAsync(UserRole? role, CancellationToken ct);
    Task<IReadOnlyList<User>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}
