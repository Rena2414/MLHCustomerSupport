namespace MlhSupport.Application.Abstractions;

public interface IAgentGateway
{
    Task<string> GetReplyAsync(Guid conversationId, string text, CancellationToken ct);
    IAsyncEnumerable<string> StreamReplyAsync(Guid conversationId, string text, CancellationToken ct);
}
