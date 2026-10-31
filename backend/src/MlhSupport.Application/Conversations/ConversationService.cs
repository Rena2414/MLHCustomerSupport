using System.Runtime.CompilerServices;
using System.Text;
using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Common;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Conversations;

public class ConversationService(IConversationRepository repository, IUserRepository users, IAgentGateway agent)
{
    private const int TitleMaxLength = 40;

    public async Task<IReadOnlyList<ConversationDto>> GetAllAsync(Guid userId, CancellationToken ct)
    {
        var conversations = await repository.GetByUserAsync(userId, ct);
        return conversations.OrderByDescending(c => c.UpdatedAt).Select(ToDto).ToList();
    }

    public async Task<ConversationDto> CreateAsync(Guid userId, CancellationToken ct)
    {
        var conversation = new Conversation { Id = Guid.NewGuid(), UserId = userId, UpdatedAt = DateTime.UtcNow };
        await repository.AddAsync(conversation, ct);
        return ToDto(conversation);
    }

    public async Task<IReadOnlyList<ChatMessageDto>?> GetMessagesAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var conversation = await GetOwnedAsync(userId, id, ct);
        return conversation?.Messages.OrderBy(m => m.CreatedAt).Select(ToDto).ToList();
    }

    public async Task<ChatMessageDto?> SendMessageAsync(Guid userId, Guid id, string text, CancellationToken ct)
    {
        var conversation = await GetOwnedAsync(userId, id, ct);
        if (conversation is null)
        {
            return null;
        }

        var user = await GetAgentUserAsync(userId, ct);
        await AppendCustomerMessageAsync(conversation, text, ct);
        var reply = await agent.GetReplyAsync(id, user, text, ct);
        var agentMessage = await AppendAsync(conversation, MessageAuthor.Agent, reply, ct);
        return ToDto(agentMessage);
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(
        Guid userId, Guid id, string text, [EnumeratorCancellation] CancellationToken ct)
    {
        var conversation = await GetOwnedAsync(userId, id, ct)
            ?? throw new NotFoundException($"Conversation {id} does not exist");

        var user = await GetAgentUserAsync(userId, ct);
        await AppendCustomerMessageAsync(conversation, text, ct);

        var reply = new StringBuilder();
        try
        {
            await foreach (var delta in agent.StreamReplyAsync(id, user, text, ct))
            {
                reply.Append(delta);
                yield return delta;
            }
        }
        finally
        {
            if (reply.Length > 0)
            {
                await AppendAsync(conversation, MessageAuthor.Agent, reply.ToString(), CancellationToken.None);
            }
        }
    }

    private async Task<Conversation?> GetOwnedAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var conversation = await repository.GetByIdAsync(id, ct);
        return conversation is not null && conversation.UserId == userId ? conversation : null;
    }

    private async Task<AgentUserContext> GetAgentUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User not found");
        return new AgentUserContext(user.Id, user.Name, user.Age);
    }

    private async Task AppendCustomerMessageAsync(Conversation conversation, string text, CancellationToken ct)
    {
        if (conversation.Title == Conversation.DefaultTitle)
        {
            conversation.Title = BuildTitle(text);
        }
        await AppendAsync(conversation, MessageAuthor.Customer, text, ct);
    }

    private async Task<ChatMessage> AppendAsync(
        Conversation conversation, MessageAuthor author, string text, CancellationToken ct)
    {
        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Author = author,
            Text = text,
            CreatedAt = DateTime.UtcNow
        };
        conversation.UpdatedAt = message.CreatedAt;
        await repository.AddMessageAsync(conversation, message, ct);
        return message;
    }

    private static string BuildTitle(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= TitleMaxLength ? trimmed : trimmed[..TitleMaxLength].TrimEnd() + "...";
    }

    private static ConversationDto ToDto(Conversation c) =>
        new(c.Id, c.Title, c.Messages.OrderBy(m => m.CreatedAt).LastOrDefault()?.Text ?? string.Empty, c.UpdatedAt);

    private static ChatMessageDto ToDto(ChatMessage m) =>
        new(m.Id, m.Author.ToString().ToLowerInvariant(), m.Text, m.CreatedAt);
}
