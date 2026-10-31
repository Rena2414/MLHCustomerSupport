using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MlhSupport.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        if (db is null)
        {
            return;
        }

        await db.Database.EnsureCreatedAsync();

        if (!await db.Customers.AnyAsync())
        {
            db.Customers.AddRange(SeedData.Customers);
            await db.SaveChangesAsync();
        }
    }
}
