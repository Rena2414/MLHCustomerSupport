using System.Runtime.CompilerServices;
using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Conversations;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Application.Tests;

public class ConversationServiceTests
{
    private class FakeRepository : IConversationRepository
    {
        public Dictionary<Guid, Conversation> Items { get; } = new();
        public Task<IReadOnlyList<Conversation>> GetAllAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Conversation>>(Items.Values.ToList());
        public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Items.GetValueOrDefault(id));
        public Task AddAsync(Conversation conversation, CancellationToken ct)
        {
            Items[conversation.Id] = conversation;
            return Task.CompletedTask;
        }
        public Task AddMessageAsync(Conversation conversation, ChatMessage message, CancellationToken ct)
        {
            conversation.Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private class FakeAgent(Func<string, string> reply) : IAgentGateway
    {
        public List<(Guid ConversationId, string Text)> Calls { get; } = new();
        public Task<string> GetReplyAsync(Guid conversationId, string text, CancellationToken ct)
        {
            Calls.Add((conversationId, text));
            return Task.FromResult(reply(text));
        }

        public async IAsyncEnumerable<string> StreamReplyAsync(
            Guid conversationId, string text, [EnumeratorCancellation] CancellationToken ct)
        {
            Calls.Add((conversationId, text));
            foreach (var piece in reply(text).Split(' ').Select((word, index) => index == 0 ? word : " " + word))
            {
                await Task.Yield();
                yield return piece;
            }
        }
    }

    private static (ConversationService Service, FakeRepository Repository, FakeAgent Agent) Build(Func<string, string>? reply = null)
    {
        var repository = new FakeRepository();
        var agent = new FakeAgent(reply ?? (text => "echo: " + text));
        return (new ConversationService(repository, agent), repository, agent);
    }

    [Fact]
    public async Task SendMessage_StoresBothMessagesAndReturnsAgentReply()
    {
        var (service, repository, agent) = Build();
        var conversation = await service.CreateAsync(CancellationToken.None);

        var reply = await service.SendMessageAsync(conversation.Id, "hello", CancellationToken.None);

        Assert.Equal("agent", reply!.Author);
        Assert.Equal("echo: hello", reply.Text);
        Assert.Equal(new[] { "customer", "agent" }, (await service.GetMessagesAsync(conversation.Id, CancellationToken.None))!.Select(m => m.Author));
        Assert.Single(agent.Calls);
        Assert.Equal(conversation.Id, agent.Calls[0].ConversationId);
        Assert.Equal(2, repository.Items[conversation.Id].Messages.Count);
    }

    [Fact]
    public async Task SendMessage_UnknownConversation_ReturnsNullWithoutCallingAgent()
    {
        var (service, _, agent) = Build();

        var reply = await service.SendMessageAsync(Guid.NewGuid(), "hello", CancellationToken.None);

        Assert.Null(reply);
        Assert.Empty(agent.Calls);
    }

    [Fact]
    public async Task FirstMessage_BecomesTitleAndLongTitlesAreShortened()
    {
        var (service, _, _) = Build();
        var conversation = await service.CreateAsync(CancellationToken.None);

        await service.SendMessageAsync(conversation.Id, new string('a', 100), CancellationToken.None);
        await service.SendMessageAsync(conversation.Id, "second", CancellationToken.None);

        var listed = (await service.GetAllAsync(CancellationToken.None)).Single();
        Assert.Equal(new string('a', 40) + "...", listed.Title);
        Assert.Equal("echo: second", listed.LastMessage);
    }

    [Fact]
    public async Task AgentFailure_KeepsCustomerMessage()
    {
        var repository = new FakeRepository();
        var failing = new ThrowingAgent();
        var service = new ConversationService(repository, failing);
        var conversation = await service.CreateAsync(CancellationToken.None);

        await Assert.ThrowsAsync<AgentUnavailableException>(
            () => service.SendMessageAsync(conversation.Id, "hello", CancellationToken.None));

        var messages = await service.GetMessagesAsync(conversation.Id, CancellationToken.None);
        Assert.Equal("hello", Assert.Single(messages!).Text);
    }

    [Fact]
    public async Task GetAll_ReturnsMostRecentlyUpdatedFirst()
    {
        var (service, _, _) = Build();
        var first = await service.CreateAsync(CancellationToken.None);
        var second = await service.CreateAsync(CancellationToken.None);

        await service.SendMessageAsync(first.Id, "bump", CancellationToken.None);

        var ids = (await service.GetAllAsync(CancellationToken.None)).Select(c => c.Id).ToList();
        Assert.Equal(new[] { first.Id, second.Id }, ids);
    }

    private class ThrowingAgent : IAgentGateway
    {
        public Task<string> GetReplyAsync(Guid conversationId, string text, CancellationToken ct) =>
            throw new AgentUnavailableException("down");

        public async IAsyncEnumerable<string> StreamReplyAsync(
            Guid conversationId, string text, [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            throw new AgentUnavailableException("down");
            yield break;
        }
    }

    [Fact]
    public async Task StreamMessage_YieldsPiecesAndStoresTheJoinedReply()
    {
        var (service, _, _) = Build(_ => "one two three");
        var conversation = await service.CreateAsync(CancellationToken.None);

        var pieces = new List<string>();
        await foreach (var piece in service.StreamMessageAsync(conversation.Id, "hi", CancellationToken.None))
        {
            pieces.Add(piece);
        }

        Assert.Equal(new[] { "one", " two", " three" }, pieces);
        var messages = (await service.GetMessagesAsync(conversation.Id, CancellationToken.None))!;
        Assert.Equal(new[] { "hi", "one two three" }, messages.Select(m => m.Text));
        Assert.Equal(new[] { "customer", "agent" }, messages.Select(m => m.Author));
    }

    [Fact]
    public async Task StreamMessage_WhenAgentFailsBeforeAnyText_KeepsOnlyCustomerMessage()
    {
        var service = new ConversationService(new FakeRepository(), new ThrowingAgent());
        var conversation = await service.CreateAsync(CancellationToken.None);

        await Assert.ThrowsAsync<AgentUnavailableException>(async () =>
        {
            await foreach (var _ in service.StreamMessageAsync(conversation.Id, "hi", CancellationToken.None))
            {
            }
        });

        var messages = await service.GetMessagesAsync(conversation.Id, CancellationToken.None);
        Assert.Equal("hi", Assert.Single(messages!).Text);
    }
}
