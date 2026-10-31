using MlhSupport.Application.Common;
using MlhSupport.Application.Conversations;
using MlhSupport.Domain.Entities;
using MlhSupport.Domain.Enums;

namespace MlhSupport.Application.Tests;

public class ConversationServiceTests
{
    private static (ConversationService Service, InMemoryStore Store, FakeAgent Agent, User Student) Build(Func<string, string>? reply = null)
    {
        var store = new InMemoryStore();
        var agent = new FakeAgent(reply);
        var student = store.AddUser("emma", UserRole.Student, age: 10);
        return (new ConversationService(store, store, agent), store, agent, student);
    }

    [Fact]
    public async Task SendMessage_StoresBothMessagesAndPassesStudentContextToAgent()
    {
        var (service, store, agent, student) = Build();
        var conversation = await service.CreateAsync(student.Id, CancellationToken.None);

        var reply = await service.SendMessageAsync(student.Id, conversation.Id, "hello", CancellationToken.None);

        Assert.Equal("agent", reply!.Author);
        Assert.Equal("echo: hello", reply.Text);
        Assert.Equal(2, store.Conversations[conversation.Id].Messages.Count);
        var call = Assert.Single(agent.Calls);
        Assert.Equal(new AgentUserContext(student.Id, student.Name, 10), call.User);
    }

    [Fact]
    public async Task OtherUsersConversation_IsInvisibleAndNotSentToAgent()
    {
        var (service, store, agent, student) = Build();
        var other = store.AddUser("liam", UserRole.Student);
        var conversation = await service.CreateAsync(student.Id, CancellationToken.None);

        Assert.Null(await service.SendMessageAsync(other.Id, conversation.Id, "hi", CancellationToken.None));
        Assert.Null(await service.GetMessagesAsync(other.Id, conversation.Id, CancellationToken.None));
        Assert.Empty(await service.GetAllAsync(other.Id, CancellationToken.None));
        Assert.Empty(agent.Calls);
    }

    [Fact]
    public async Task FirstMessage_BecomesTitleAndLongTitlesAreShortened()
    {
        var (service, _, _, student) = Build();
        var conversation = await service.CreateAsync(student.Id, CancellationToken.None);

        await service.SendMessageAsync(student.Id, conversation.Id, new string('a', 100), CancellationToken.None);
        await service.SendMessageAsync(student.Id, conversation.Id, "second", CancellationToken.None);

        var listed = (await service.GetAllAsync(student.Id, CancellationToken.None)).Single();
        Assert.Equal(new string('a', 40) + "...", listed.Title);
        Assert.Equal("echo: second", listed.LastMessage);
    }

    [Fact]
    public async Task AgentFailure_KeepsStudentMessage()
    {
        var (service, _, agent, student) = Build();
        agent.Fail = true;
        var conversation = await service.CreateAsync(student.Id, CancellationToken.None);

        await Assert.ThrowsAsync<AgentUnavailableException>(
            () => service.SendMessageAsync(student.Id, conversation.Id, "hello", CancellationToken.None));

        var messages = await service.GetMessagesAsync(student.Id, conversation.Id, CancellationToken.None);
        Assert.Equal("hello", Assert.Single(messages!).Text);
    }

    [Fact]
    public async Task StreamMessage_YieldsPiecesAndStoresTheJoinedReply()
    {
        var (service, _, _, student) = Build(_ => "one two three");
        var conversation = await service.CreateAsync(student.Id, CancellationToken.None);

        var pieces = new List<string>();
        await foreach (var piece in service.StreamMessageAsync(student.Id, conversation.Id, "hi", CancellationToken.None))
        {
            pieces.Add(piece);
        }

        Assert.Equal(new[] { "one", " two", " three" }, pieces);
        var messages = (await service.GetMessagesAsync(student.Id, conversation.Id, CancellationToken.None))!;
        Assert.Equal(new[] { "hi", "one two three" }, messages.Select(m => m.Text));
    }

    [Fact]
    public async Task StreamMessage_ForeignConversation_Throws()
    {
        var (service, store, _, student) = Build();
        var other = store.AddUser("liam", UserRole.Student);
        var conversation = await service.CreateAsync(student.Id, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await foreach (var _ in service.StreamMessageAsync(other.Id, conversation.Id, "hi", CancellationToken.None))
            {
            }
        });
    }
}
