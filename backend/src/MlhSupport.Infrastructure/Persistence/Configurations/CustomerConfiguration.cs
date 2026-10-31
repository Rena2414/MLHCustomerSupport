using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.Plan).HasMaxLength(100).IsRequired();
        builder.Property(c => c.SubscriptionStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.NextPaymentAmount).HasPrecision(10, 2);

        builder.HasData(SeedData.Customers);
    }
}
