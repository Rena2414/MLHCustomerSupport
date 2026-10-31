using Microsoft.Extensions.DependencyInjection;
using MlhSupport.Application.Conversations;
using MlhSupport.Application.Customers;

namespace MlhSupport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CustomerService>();
        services.AddScoped<ConversationService>();
        return services;
    }
}
