using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MlhSupport.Api.Web;
using MlhSupport.Application.Conversations;

namespace MlhSupport.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Student)]
[Route("api/[controller]")]
public class ConversationsController(ConversationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetAll(CancellationToken ct) =>
        Ok(await service.GetAllAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<ConversationDto>> Create(CancellationToken ct) =>
        Ok(await service.CreateAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetMessages(Guid id, CancellationToken ct)
    {
        var messages = await service.GetMessagesAsync(User.GetUserId(), id, ct);
        return messages is null ? NotFound() : Ok(messages);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<ChatMessageDto>> Send(Guid id, SendMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 4000)
        {
            return BadRequest("The message must be between 1 and 4000 characters");
        }

        try
        {
            var reply = await service.SendMessageAsync(User.GetUserId(), id, request.Text, ct);
            return reply is null ? NotFound() : Ok(reply);
        }
        catch (AgentUnavailableException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost("{id:guid}/messages/stream")]
    public async Task Stream(Guid id, SendMessageRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 4000)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (await service.GetMessagesAsync(User.GetUserId(), id, ct) is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var delta in service.StreamMessageAsync(User.GetUserId(), id, request.Text, ct))
            {
                await WriteEventAsync("delta", new { text = delta }, ct);
            }
            await WriteEventAsync("done", new { }, ct);
        }
        catch (AgentUnavailableException ex)
        {
            await WriteEventAsync("error", new { message = ex.Message }, CancellationToken.None);
        }
    }

    private async Task WriteEventAsync(string name, object data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {name}\ndata: {JsonSerializer.Serialize(data)}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
