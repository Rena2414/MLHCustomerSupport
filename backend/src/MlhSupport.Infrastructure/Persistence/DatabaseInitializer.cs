using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MlhSupport.Application.Abstractions;

namespace MlhSupport.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await SeedData.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>(), CancellationToken.None);
    }
}
