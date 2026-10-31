using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MlhSupport.Application.Abstractions;
using MlhSupport.Infrastructure.Persistence;

namespace MlhSupport.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
            return services;
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        return services;
    }
}
