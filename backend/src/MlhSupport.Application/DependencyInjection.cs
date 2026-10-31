using Microsoft.Extensions.DependencyInjection;
using MlhSupport.Application.Customers;

namespace MlhSupport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CustomerService>();
        return services;
    }
}
