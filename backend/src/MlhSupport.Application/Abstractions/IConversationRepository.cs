using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Abstractions;

public interface IConversationRepository
{
    Task<IReadOnlyList<Conversation>> GetByUserAsync(Guid userId, CancellationToken ct);
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Conversation conversation, CancellationToken ct);
    Task AddMessageAsync(Conversation conversation, ChatMessage message, CancellationToken ct);
}
