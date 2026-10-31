namespace MlhSupport.Application.Conversations;

public class AgentUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
