using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MlhSupport.Application.Abstractions;
using MlhSupport.Infrastructure.Agents;
using MlhSupport.Infrastructure.Persistence;

namespace MlhSupport.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"ConnectionStrings:{ConnectionStringName} is not configured. " +
                "Copy src/MlhSupport.Api/secrets.example.json to src/MlhSupport.Api/secrets.json and adjust it.");

        var agentOptions = configuration.GetSection(AgentServiceOptions.SectionName).Get<AgentServiceOptions>()
            ?? new AgentServiceOptions();

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<IConversationRepository, EfConversationRepository>();
        services.AddHttpClient<IAgentGateway, HttpAgentGateway>(client =>
        {
            client.BaseAddress = new Uri(agentOptions.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(agentOptions.TimeoutSeconds);
        });
        return services;
    }
}
