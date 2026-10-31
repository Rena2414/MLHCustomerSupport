namespace MlhSupport.Domain.Entities;

public class Conversation
{
    public const string DefaultTitle = "New conversation";

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = DefaultTitle;
    public DateTime UpdatedAt { get; set; }
    public List<ChatMessage> Messages { get; set; } = [];
}
