using Microsoft.Extensions.DependencyInjection;
using MlhSupport.Application.Abstractions;
using MlhSupport.Infrastructure.Persistence;

namespace MlhSupport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        return services;
    }
}
