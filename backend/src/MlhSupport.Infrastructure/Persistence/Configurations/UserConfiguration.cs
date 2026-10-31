using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MlhSupport.Domain.Entities;

namespace MlhSupport.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Login).HasMaxLength(40).IsRequired();
        builder.HasIndex(u => u.Login).IsUnique();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.SubscriptionType).HasConversion<int>();
        builder.HasIndex(u => u.Role);
    }
}

public class ParentChildConfiguration : IEntityTypeConfiguration<ParentChild>
{
    public void Configure(EntityTypeBuilder<ParentChild> builder)
    {
        builder.ToTable("parent_children");
        builder.HasKey(p => new { p.ParentId, p.ChildId });
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.ParentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.ChildId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(p => p.ChildId);
    }
}
