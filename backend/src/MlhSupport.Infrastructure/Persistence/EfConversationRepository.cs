using Microsoft.EntityFrameworkCore;
using MlhSupport.Application.Abstractions;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class EfConversationRepository(AppDbContext db) : IConversationRepository
{
    public async Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken ct) =>
        await db.Conversations.AsNoTracking().Include(c => c.Messages).ToListAsync(ct);

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Conversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Conversation conversation, CancellationToken ct)
    {
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddMessageAsync(Conversation conversation, ChatMessage message, CancellationToken ct)
    {
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);
    }
}
