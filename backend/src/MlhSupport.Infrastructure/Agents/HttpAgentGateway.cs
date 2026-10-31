using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MlhSupport.Application.Abstractions;
using MlhSupport.Application.Conversations;

namespace MlhSupport.Infrastructure.Agents;

public class HttpAgentGateway(HttpClient http) : IAgentGateway
{
    public async Task<string> GetReplyAsync(Guid conversationId, string text, CancellationToken ct)
    {
        try
        {
            var response = await http.PostAsJsonAsync("chat", new AgentChatRequest(conversationId.ToString(), text), ct);
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
        Guid conversationId, string text, [EnumeratorCancellation] CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/stream")
        {
            Content = JsonContent.Create(new AgentChatRequest(conversationId.ToString(), text))
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

    private record AgentDelta([property: JsonPropertyName("text")] string Text);

    private record AgentChatRequest(
        [property: JsonPropertyName("conversation_id")] string ConversationId,
        [property: JsonPropertyName("message")] string Message);

    private record AgentChatResponse(
        [property: JsonPropertyName("conversation_id")] string ConversationId,
        [property: JsonPropertyName("reply")] string Reply);
}
