using Microsoft.EntityFrameworkCore;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FullName).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(320).IsRequired();
            entity.Property(c => c.Plan).HasMaxLength(100).IsRequired();
            entity.Property(c => c.NextPaymentAmount).HasPrecision(10, 2);
            entity.Property(c => c.NextPaymentDate).HasColumnType("timestamp without time zone");
            entity.Property(c => c.CreatedAt).HasColumnType("timestamp without time zone");
        });
    }
}
