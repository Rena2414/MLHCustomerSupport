namespace MlhSupport.Application.Conversations;

public record ConversationDto(Guid Id, string Title, string LastMessage, DateTime UpdatedAt);

public record ChatMessageDto(Guid Id, string Author, string Text, DateTime CreatedAt);

public record SendMessageRequest(string Text);

public record AgentUserContext(Guid Id, string Name, int Age);
