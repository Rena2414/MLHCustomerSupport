using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Analytics;
using MlhSupport.Application.Conversations;

namespace MlhSupport.Infrastructure.Agents;

public class HttpAgentGateway(HttpClient http) : IAgentGateway
{
    public async Task<string> GetReplyAsync(Guid conversationId, AgentUserContext user, string text, CancellationToken ct)
    {
        try
        {
            var response = await http.PostAsJsonAsync("chat", BuildRequest(conversationId, user, text), ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<AgentChatResponse>(ct);
            return body?.Reply ?? throw new AgentUnavailableException("The agent returned an empty response");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new AgentUnavailableException("The agent service is not available", ex);
        }
    }

    public async IAsyncEnumerable<string> StreamReplyAsync(
        Guid conversationId, AgentUserContext user, string text, [EnumeratorCancellation] CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/stream")
        {
            Content = JsonContent.Create(BuildRequest(conversationId, user, text))
        };

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new AgentUnavailableException("The agent service is not available", ex);
        }

        using (response)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            string? eventName = null;
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                if (line.StartsWith("event: "))
                {
                    eventName = line["event: ".Length..];
                }
                else if (line.StartsWith("data: "))
                {
                    var data = line["data: ".Length..];
                    if (eventName == "delta")
                    {
                        var delta = JsonSerializer.Deserialize<AgentDelta>(data)?.Text;
                        if (!string.IsNullOrEmpty(delta))
                        {
                            yield return delta;
                        }
                    }
                    else if (eventName == "error")
                    {
                        throw new AgentUnavailableException("The agent could not produce an answer");
                    }
                }
                else if (line.Length == 0)
                {
                    eventName = null;
                }
            }
        }
    }

    public async Task<TeacherSummaryDto> SummarizeTeacherAsync(TeacherSummaryRequest request, CancellationToken ct)
    {
        var body = new AgentSummaryRequest(
            request.TeacherName,
            request.Concerns.Select(c => new AgentSummaryConcern(c.Text, c.Category, c.Severity, c.CreatedAt)).ToList());
        try
        {
            var response = await http.PostAsJsonAsync("analytics/teacher-summary", body, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AgentSummaryResponse>(ct)
                ?? throw new AgentUnavailableException("The agent returned an empty summary");
            return new TeacherSummaryDto(result.Summary, result.RecommendedActions ?? [], DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            throw new AgentUnavailableException("The agent service is not available", ex);
        }
    }

    private static AgentChatRequest BuildRequest(Guid conversationId, AgentUserContext user, string text) =>
        new(conversationId.ToString(), text, new AgentUser(user.Id.ToString(), user.Name, user.Age));

    private record AgentUser(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("age")] int Age);

    private record AgentSummaryConcern(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("severity")] string Severity,
        [property: JsonPropertyName("created_at")] DateTime CreatedAt);

    private record AgentSummaryRequest(
        [property: JsonPropertyName("teacher_name")] string TeacherName,
        [property: JsonPropertyName("concerns")] IReadOnlyList<AgentSummaryConcern> Concerns);

    private record AgentSummaryResponse(
        [property: JsonPropertyName("summary")] string Summary,
        [property: JsonPropertyName("recommended_actions")] IReadOnlyList<string>? RecommendedActions);

    private record AgentDelta([property: JsonPropertyName("text")] string Text);

    private record AgentChatRequest(
        [property: JsonPropertyName("conversation_id")] string ConversationId,
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("user")] AgentUser User);

    private record AgentChatResponse(
        [property: JsonPropertyName("conversation_id")] string ConversationId,
        [property: JsonPropertyName("reply")] string Reply);
}
