namespace MlhSupport.Infrastructure.Agents;

public class AgentServiceOptions
{
    public const string SectionName = "AgentService";

    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 240;
}
