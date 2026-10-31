using MlhSupport.Application.Analytics;
using MlhSupport.Application.Conversations;

namespace MlhSupport.Application.Abstractions;

public interface IAgentGateway
{
    Task<string> GetReplyAsync(Guid conversationId, AgentUserContext user, string text, CancellationToken ct);
    IAsyncEnumerable<string> StreamReplyAsync(Guid conversationId, AgentUserContext user, string text, CancellationToken ct);
    Task<TeacherSummaryDto> SummarizeTeacherAsync(TeacherSummaryRequest request, CancellationToken ct);
}
